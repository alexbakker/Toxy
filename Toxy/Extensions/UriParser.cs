using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Toxy.Extensions
{
    /* stolen from http://stackoverflow.com/a/867455 */
    public static class UriParser
    {
        //TODO: find a better regex
        private static readonly Regex regex = new Regex(@"(?#Protocol)(?:(?:ht|f)tp(?:s?)\:\/\/|~/|/)?(?#Username:Password)(?:\w+:\w+@)?(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[\d]{1,5})?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?");

        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(UriParser),
            new PropertyMetadata(null, OnTextChanged)
        );

        public static string GetText(DependencyObject d)
        { return d.GetValue(TextProperty) as string; }

        public static void SetText(DependencyObject d, string value)
        { d.SetValue(TextProperty, value); }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as TextBlock;
            if (textBlock == null)
                return;

            textBlock.Inlines.Clear();

            string newText = (string)e.NewValue;
            if (string.IsNullOrEmpty(newText))
                return;

            int lastPos = 0;
            foreach (Match match in regex.Matches(newText))
            {
                if (match.Index != lastPos)
                {
                    var rawRext = newText.Substring(lastPos, match.Index - lastPos);
                    textBlock.Inlines.Add(new Run(rawRext));
                }

                var link = new Hyperlink(new Run(match.Value));
                link.Click += OnUrlClicked;

                //for now, just put http:// in front of it if it's not a valid uri
                try { link.NavigateUri = new Uri(match.Value); }
                catch { link.NavigateUri = new Uri("http://" + match.Value); }

                textBlock.Inlines.Add(link);
                lastPos = match.Index + match.Length;
            }

            if (lastPos < newText.Length)
                textBlock.Inlines.Add(new Run(newText.Substring(lastPos)));
        }

        private static void OnUrlClicked(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;
            string uri = link.NavigateUri.ToString();

            try { Process.Start(uri); }
            catch (Exception ex) { Debugging.Write(string.Format("Could not open {0}:{1}", uri, ex.ToString())); }
        }
    }
}
