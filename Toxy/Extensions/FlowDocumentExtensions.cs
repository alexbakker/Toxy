using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.IO;
using System.Reflection;
using System.Windows.Markup;

using MahApps.Metro.Controls;
using SharpTox.Core;

using Toxy.Common;
using Toxy.Views;
using Toxy.Extenstions;
using Toxy.Common.Transfers;

namespace Toxy.Extenstions
{
    static class FlowDocumentExtensions
    {
        public static void AddNewMessageRow(this FlowDocument document, Tox tox, MessageData data, bool sameUser)
        {
            document.IsEnabled = true;

            //Make a new row
            TableRow newTableRow = new TableRow();
            newTableRow.Tag = data;

            //Make a new cell and create a paragraph in it
            TableCell usernameTableCell = new TableCell();
            usernameTableCell.Name = "usernameTableCell";
            usernameTableCell.Padding = new Thickness(10, 0, 0, 0);

            Paragraph usernameParagraph = new Paragraph();
            usernameParagraph.TextAlignment = data.IsAction ? TextAlignment.Right : TextAlignment.Left;
            usernameParagraph.Foreground = new SolidColorBrush(Color.FromRgb(164, 164, 164));

            if (data.Username != tox.Name.Replace("\n", "").Replace("\r", ""))
                usernameParagraph.SetResourceReference(Paragraph.ForegroundProperty, "AccentColorBrush");

            if(!sameUser)
                usernameParagraph.Inlines.Add(data.Username);

            usernameTableCell.Blocks.Add(usernameParagraph);

            //Make a new cell and create a paragraph in it
            TableCell messageTableCell = new TableCell();
            Paragraph messageParagraph = new Paragraph();
            messageParagraph.TextAlignment = TextAlignment.Left;

            if (!data.IsGroupMsg && data.IsSelf)
                messageParagraph.Foreground = Brushes.LightGray;

            bool isHighlight = data.IsGroupMsg && !data.IsSelf && data.Message.ToLower().Contains(tox.Name.ToLower());
            ProcessMessage(data, messageParagraph, false, isHighlight);

            messageTableCell.Blocks.Add(messageParagraph);

            TableCell timestampTableCell = new TableCell();
            Paragraph timestampParagraph = new Paragraph();
            timestampParagraph.Foreground = Brushes.LightGray;
            timestampTableCell.TextAlignment = TextAlignment.Right;
            timestampParagraph.Inlines.Add(data.Timestamp.ToShortTimeString());
            timestampTableCell.Blocks.Add(timestampParagraph);                

            //Add the two cells to the row we made before
            newTableRow.Cells.Add(usernameTableCell);
            newTableRow.Cells.Add(messageTableCell);
            newTableRow.Cells.Add(timestampTableCell);

            //Adds row to the Table > TableRowGroup
            TableRowGroup MessageRows = (TableRowGroup)document.FindName("MessageRows");
            MessageRows.Rows.Add(newTableRow);
        }

        public static FileTransferControl AddNewFileTransfer(this FlowDocument doc, Tox tox, FileTransfer transfer)
        {
            var fileTableCell = new TableCell();
            var fileTransferControl = new FileTransferControl(transfer, fileTableCell);

            var usernameParagraph = new Section();
            var newTableRow = new TableRow();
            newTableRow.Tag = transfer;

            var fileTransferContainer = new BlockUIContainer();
            fileTransferControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            fileTransferControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            fileTransferContainer.Child = fileTransferControl;

            usernameParagraph.Blocks.Add(fileTransferContainer);
            usernameParagraph.Padding = new Thickness(0);

            
            fileTableCell.ColumnSpan = 3;
            fileTableCell.Blocks.Add(usernameParagraph);
            newTableRow.Cells.Add(fileTableCell);
            fileTableCell.Padding = new Thickness(0, 10, 0, 10);

            var MessageRows = (TableRowGroup)doc.FindName("MessageRows");
            MessageRows.Rows.Add(newTableRow);

            return fileTransferControl;
        }

        static void ProcessMessage(MessageData data, Paragraph messageParagraph, bool append, bool isBold)
        {
            List<string> urls = new List<string>();
            List<int> indices = new List<int>();
            string[] parts = data.Message.Split(' ');

            foreach (string part in parts)
            {
                if (Regex.IsMatch(part, @"(((file|gopher|news|nntp|telnet|http|ftp|https|ftps|sftp)://)|(www\.))+(([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(/[a-zA-Z0-9\&amp;%_\./-~-]*)?", RegexOptions.IgnoreCase)/*Regex.IsMatch(part, @"^(http|https|ftp|)\://|[a-zA-Z0-9\-\.]+\.[a-zA-Z](:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]$")*/)
                    urls.Add(part);
            }

            if (urls.Count > 0)
            {
                foreach (string url in urls)
                {
                    indices.Add(data.Message.IndexOf(url));
                    data.Message = data.Message.Replace(url, "");
                }

                messageParagraph.AddMessage(data.Message, append, isBold);
                Inline inline = messageParagraph.Inlines.LastInline;

                for (int i = indices.Count; i-- > 0; )
                {
                    string url = urls[i];
                    int index = append ? indices[i] + 1 : indices[i];

                    Run run = new Run(url);
                    TextPointer pointer = new TextRange(inline.ContentStart, inline.ContentEnd).Text == "\n" ? inline.ContentEnd : inline.ContentStart;

                    Hyperlink link = new Hyperlink(run, pointer.GetPositionAtOffset(index));
                    link.IsEnabled = true;
                    link.Click += delegate(object sender, RoutedEventArgs args)
                    {
                        try { Process.Start(url); }
                        catch { }
                    };
                }
            }
            else
            {
                messageParagraph.AddMessage(data.Message, append, isBold);
            }
        }

        private static void AddMessage(this Paragraph p, string message, bool append, bool isBold)
        {
            if (!isBold)
            {
                if (!append)
                    p.Inlines.Add(message);
                else
                    p.Inlines.Add("\n" + message);
            }
            else
            {
                if (!append)
                {
                    var bold = new Bold();
                    bold.Inlines.Add(message);
                    p.Inlines.Add(bold);
                }
                else
                {
                    var bold = new Bold();
                    bold.Inlines.Add("\n" + message);
                    p.Inlines.Add(bold);
                }
            }
        }

        public static TableRow GetLastMessageRun(this FlowDocument doc)
        {
            try
            {
                return doc.FindChildren<TableRow>().Last(t => !(t.Tag is FileTransfer));
            }
            catch
            {
                return null;
            }
        }

        public static ScrollViewer FindScrollViewer(this FlowDocumentScrollViewer viewer)
        {
            if (VisualTreeHelper.GetChildrenCount(viewer) == 0)
                return null;

            DependencyObject first = VisualTreeHelper.GetChild(viewer, 0);
            if (first == null)
                return null;

            Decorator border = (Decorator)VisualTreeHelper.GetChild(first, 0);
            if (border == null)
                return null;

            return (ScrollViewer)border.Child;
        }

        public static FlowDocument CreateNewDocument()
        {
            Stream doc_stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Toxy.Views.Message.xaml");
            FlowDocument doc = (FlowDocument)XamlReader.Load(doc_stream);
            doc.IsEnabled = true;

            return doc;
        }
    }
}
