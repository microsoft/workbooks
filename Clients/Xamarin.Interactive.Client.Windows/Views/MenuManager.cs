//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xamarin.Interactive.Client.ViewInspector;
using Xamarin.Interactive.Client.Windows.Commands;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.SystemInformation;
using Xamarin.Interactive.Workbook.Structure;

namespace Xamarin.Interactive.Client.Windows.Views
{
    class MenuManager
    {
        readonly Menu rootMenu;
        readonly MenuItem fileMenu;
        readonly MenuItem openRecentMenu;
        readonly Window window;
        readonly bool includeRunMenu;

        public MenuManager (Menu rootMenu, Window window, bool includeRunMenu = false)
        {
            if (rootMenu == null)
                throw new ArgumentNullException (nameof (rootMenu));
            if (window == null)
                throw new ArgumentNullException (nameof (window));

            this.rootMenu = rootMenu;
            this.window = window;
            this.includeRunMenu = includeRunMenu;

            fileMenu = new MenuItem {
                Header = Catalog.GetString ("_File")
            };

            openRecentMenu = new MenuItem {
                Header = Catalog.GetString ("Open Recent"),
                Visibility = Visibility.Collapsed
            };

            Update ();

            if (App.RecentDocuments != null) {
                UpdateOpenRecent ();
                App.RecentDocuments.CollectionChanged += (sender, e) => UpdateOpenRecent ();
            }
        }

        public void AddMenu (MenuItem menu)
        {
            rootMenu.Items.Insert (rootMenu.Items.Count - 1, menu);
        }

        public void RemoveMenu (MenuItem menu)
        {
            rootMenu.Items.Remove (menu);
        }

        public void Update (IEditor editor = null)
        {
            rootMenu.Items.Clear ();
            fileMenu.Items.Clear ();

            // File
            if (ClientInfo.Flavor == ClientFlavor.Workbooks) {
                if (editor != null)
                    fileMenu.Items.Add (new MenuItem { Command = ApplicationCommands.New });

                fileMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Open });
                fileMenu.Items.Add (openRecentMenu);

                if (editor != null) {
                    fileMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Save });
                    fileMenu.Items.Add (new MenuItem { Command = ApplicationCommands.SaveAs });
                    fileMenu.Items.Add (new Separator ());
                    fileMenu.Items.Add (new MenuItem { Command = NuGetPackagesNode.AddPackage });
                    fileMenu.Items.Add (new Separator ());
                }
            }
            fileMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Close });
            rootMenu.Items.Add (fileMenu);

            if (editor != null) {
                // TODO: Re-enable when edit commands display properly for web editors
                //// Edit
                //var editMenu = new MenuItem { Header = "_Edit" };
                //editMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Undo });
                //editMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Redo });
                //editMenu.Items.Add (new Separator ());
                //editMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Cut });
                //editMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Copy });
                //editMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Paste });
                //editMenu.Items.Add (new MenuItem { Command = ApplicationCommands.Delete });
                //editMenu.Items.Add (new MenuItem { Command = ApplicationCommands.SelectAll });
                //rootMenu.Items.Add (editMenu);

                // Insert
                var insertMenu = new MenuItem { Header = "_Insert" };
                TryAddMenuItem (insertMenu, editor, "insertImage");
                TryAddMenuItem (insertMenu, editor, "insertHorizontalRule");
                if (insertMenu.Items.Count > 0)
                    rootMenu.Items.Add (insertMenu);

                // Format
                var formatMenu = new MenuItem {Header = "Fo_rmat"};

                var headingMenu = new MenuItem {Header = "Heading"};
                TryAddMenuItem (headingMenu, editor, "heading1");
                TryAddMenuItem (headingMenu, editor, "heading2");
                TryAddMenuItem (headingMenu, editor, "heading3");
                TryAddMenuItem (headingMenu, editor, "heading4");
                TryAddMenuItem (headingMenu, editor, "heading5");
                TryAddMenuItem (headingMenu, editor, "heading6");
                if (headingMenu.Items.Count > 0)
                    formatMenu.Items.Add (headingMenu);

                TryAddMenuItem (formatMenu, editor, "toggleStrong");
                TryAddMenuItem (formatMenu, editor, "toggleEm");
                TryAddMenuItem (formatMenu, editor, "toggleCode");
                TryAddMenuItem (formatMenu, editor, "toggleLink");

                if (formatMenu.Items.Count > 0)
                    rootMenu.Items.Add (formatMenu);
            }

            if (includeRunMenu) {
                var runMenu = new MenuItem { Header = "_Run" };
                runMenu.Items.Add (new MenuItem {
                    Header = "Run All",
                    Command = Commands.Commands.ExecuteAll,
                });
                rootMenu.Items.Add (runMenu);
            }

            // Tools
            var toolsMenu = new MenuItem { Header = "_Tools" };
            toolsMenu.Items.Add (new MenuItem {
                Header = "Preferences…",
                Command = Commands.Commands.ShowOptions,
            });
            rootMenu.Items.Add (toolsMenu);

            // Help
            var helpMenu = new MenuItem { Header = "_Help" };

            if (ClientInfo.Flavor == ClientFlavor.Workbooks) {
                var tutorialsItem = new MenuItem {
                    Header = "Tutorials"
                };

                try {
                    var workbookFiles = App.AppDirectory
                        .Combine ("Workbooks")
                        .EnumerateFiles ("*.workbook", SearchOption.TopDirectoryOnly);

                    foreach (var workbookFile in workbookFiles)
                        tutorialsItem.Items.Add (new MenuItem {
                            Header = workbookFile.NameWithoutExtension,
                            Command = Commands.Commands.OpenFile,
                            CommandParameter = workbookFile
                        });
                } catch (Exception e) {
                    Log.Error (nameof(MenuManager), e);
                }

                if (tutorialsItem.Items.Count > 0)
                    tutorialsItem.Items.Add (new Separator ());

                tutorialsItem.Items.Add (new MenuItem {
                    Header = ClientInfo.DownloadWorkbooksMenuLabel,
                    Command = Commands.Commands.OpenSampleWorkbooks
                });

                helpMenu.Items.Add (tutorialsItem);
            }

            helpMenu.Items.Add (new MenuItem {
                Header = "Support",
                Command = Commands.Commands.Help,
            });

            if (ClientInfo.Flavor != ClientFlavor.Inspector)
                helpMenu.Items.Add (new MenuItem {
                    Header = "Check for Updates",
                    Command = Commands.Commands.CheckForUpdates,
                    CommandParameter = window
                });

            helpMenu.Items.Add (new Separator ());

            helpMenu.Items.Add (new MenuItem {
                Header = Catalog.GetString ("Reveal Log File"),
                Command = new DelegateCommand (_ =>
                    Process.Start ("explorer.exe", $"/select,\"{ClientApp.SharedInstance.Paths.SessionLogFile}\"")),
            });

            helpMenu.Items.Add (new MenuItem {
                Header = Catalog.GetString ("Copy Version Information"),
                Command = new DelegateCommand (_ =>
                    Clipboard.SetText (ClientApp.SharedInstance.IssueReport.GetEnvironmentMarkdown ())),
            });

            helpMenu.Items.Add (new Separator ());

            helpMenu.Items.Add (new MenuItem {
                Header = Catalog.GetString ("Report an Issue…"),
                Command = new DelegateCommand (_ =>
                    Process.Start (ClientApp.SharedInstance.IssueReport.GetIssueReportUrlForGitHub ())),
            });

            helpMenu.Items.Add (new Separator ());

            helpMenu.Items.Add (new MenuItem {
                Header = "About " + ClientInfo.FullProductName,
                Command = Commands.Commands.About,
                CommandParameter = window,
            });
            rootMenu.Items.Add (helpMenu);
        }

        void UpdateOpenRecent ()
        {
            openRecentMenu.Items.Clear ();

            foreach (var document in App.RecentDocuments) {
                openRecentMenu.Items.Add (new MenuItem {
                    Header = document.Title ?? Path.GetFileName (document.Path),
                    ToolTip = document.Path,
                    Command = Commands.Commands.OpenFile,
                    CommandParameter = document.Path
                });
            }

            if (openRecentMenu.Items.Count > 0) {
                openRecentMenu.Items.Add (new Separator ());
                openRecentMenu.Items.Add (new MenuItem {
                    Header = Catalog.GetString ("Clear"),
                    Command = Commands.Commands.ClearRecentFiles
                });

                openRecentMenu.Visibility = Visibility.Visible;
            } else {
                openRecentMenu.Visibility = Visibility.Collapsed;
            }
        }

        static bool TryAddMenuItem (MenuItem parentMenu, IEditor editor, string commandId)
        {
            EditorCommand editorCommand;
            if (!editor.TryGetCommand (commandId, out editorCommand))
                return false;

            var command = new EditorCommandWrapper ();
            command.Update (editor, editorCommand);
            var menuItem = new MenuItem {
                Header = editorCommand.Title,
                Command = command,
            };

            parentMenu.Items.Add (menuItem);
            return true;
        }

        class EditorCommandWrapper : ICommand
        {
            IEditor editor;
            EditorCommand editorCommand;
            EditorCommandStatus status;

            public void Update (IEditor editor, EditorCommand editorCommand)
            {
                this.editor = editor;
                this.editorCommand = editorCommand;

                if (editor == null) {
                    editorCommand = default(EditorCommand);
                    status = EditorCommandStatus.Unsupported;
                } else
                    status = editor.GetCommandStatus (editorCommand);

                CanExecuteChanged?.Invoke (this, EventArgs.Empty);
            }

            public bool CanExecute (object parameter) => status == EditorCommandStatus.Enabled;

            public void Execute (object parameter) => editor?.ExecuteCommand (editorCommand);

            public event EventHandler CanExecuteChanged;
        }
    }
}