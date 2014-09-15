using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MahApps.Metro.Controls;
using SharpTox.Core;
using Toxy.Views;

namespace Toxy.Common
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

            if (data.Username != tox.GetSelfName())
                usernameParagraph.SetResourceReference(Paragraph.ForegroundProperty, "AccentColorBrush");

            if(!sameUser)
                usernameParagraph.Inlines.Add(data.Username);

            usernameTableCell.Blocks.Add(usernameParagraph);

            //Make a new cell and create a paragraph in it
            TableCell messageTableCell = new TableCell();
            Paragraph messageParagraph = new Paragraph();
            messageParagraph.TextAlignment = TextAlignment.Left;

            if (data.IsSelf)
                messageParagraph.Foreground = new SolidColorBrush(Color.FromRgb(164, 164, 164));

            ProcessMessage(data, messageParagraph, false);

            //messageParagraph.Inlines.Add(fakeHyperlink);
            messageTableCell.Blocks.Add(messageParagraph);

            TableCell timestampTableCell = new TableCell();
            Paragraph timestamParagraph = new Paragraph();
            timestampTableCell.TextAlignment = TextAlignment.Right;
            timestamParagraph.Inlines.Add(DateTime.Now.ToShortTimeString());
            timestampTableCell.Blocks.Add(timestamParagraph);
            timestamParagraph.Foreground = new SolidColorBrush(Color.FromRgb(164, 164, 164));
            //Add the two cells to the row we made before
            newTableRow.Cells.Add(usernameTableCell);
            newTableRow.Cells.Add(messageTableCell);
            newTableRow.Cells.Add(timestampTableCell);

            //Adds row to the Table > TableRowGroup
            TableRowGroup MessageRows = (TableRowGroup)document.FindName("MessageRows");
            MessageRows.Rows.Add(newTableRow);
        }

        public static FileTransfer AddNewFileTransfer(this FlowDocument doc, Tox tox, int friendnumber, int filenumber, string filename, ulong filesize, bool is_sender)
        {
            FileTransferControl fileTransferControl = new FileTransferControl(tox.GetName(friendnumber), friendnumber, filenumber, filename, filesize);
            FileTransfer transfer = new FileTransfer() { FriendNumber = friendnumber, FileNumber = filenumber, FileName = filename, FileSize = filesize, IsSender = is_sender, Control = fileTransferControl };

            Section usernameParagraph = new Section();
            TableRow newTableRow = new TableRow();
            newTableRow.Tag = transfer;

            BlockUIContainer fileTransferContainer = new BlockUIContainer();
            fileTransferControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            fileTransferControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            fileTransferContainer.Child = fileTransferControl;

            usernameParagraph.Blocks.Add(fileTransferContainer);
            usernameParagraph.Padding = new Thickness(0);

            TableCell fileTableCell = new TableCell();
            fileTableCell.ColumnSpan = 2;
            fileTableCell.Blocks.Add(usernameParagraph);
            newTableRow.Cells.Add(fileTableCell);
            fileTableCell.Padding = new Thickness(0, 10, 0, 10);

            TableRowGroup MessageRows = (TableRowGroup)doc.FindName("MessageRows");
            MessageRows.Rows.Add(newTableRow);

            return transfer;
        }

        static void ProcessMessage(MessageData data, Paragraph messageParagraph, bool append)
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

                if (!append)
                    messageParagraph.Inlines.Add(data.Message);
                else
                    messageParagraph.Inlines.Add("\n" + data.Message);
                
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
                        catch
                        {
                            try { Process.Start("http://" + url); }
                            catch { }
                        }
                    };
                }
            }
            else
            {
                if (!append)
                    messageParagraph.Inlines.Add(data.Message);
                else
                    messageParagraph.Inlines.Add("\n" + data.Message);
            }
        }
    }
}
