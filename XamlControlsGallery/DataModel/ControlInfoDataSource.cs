//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app
// is first launched.

namespace AppUIBasics.Data
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class ControlInfoDataItem
    {
        public ControlInfoDataItem(string uniqueId, string title, string subtitle, string imagePath, string badgeString, string description, string content, bool isNew, bool isUpdated, bool isPreview)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.BadgeString = badgeString;
            this.Content = content;
            this.IsNew = isNew;
            this.IsUpdated = isUpdated;
            this.IsPreview = isPreview;
            this.Docs = new ObservableCollection<ControlInfoDocLink>();
            this.RelatedControls = new ObservableCollection<string>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string BadgeString { get; private set; }
        public string Content { get; private set; }
        public bool IsNew { get; private set; }
        public bool IsUpdated { get; private set; }
        public bool IsPreview { get; private set; }
        public ObservableCollection<ControlInfoDocLink> Docs { get; private set; }
        public ObservableCollection<string> RelatedControls { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    public class ControlInfoDocLink
    {
        public ControlInfoDocLink(string title, string uri)
        {
            this.Title = title;
            this.Uri = uri;
        }
        public string Title { get; private set; }
        public string Uri { get; private set; }
    }


    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class ControlInfoDataGroup
    {
        public ControlInfoDataGroup(string uniqueId, string title, string subtitle, string imagePath, string description)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<ControlInfoDataItem>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public ObservableCollection<ControlInfoDataItem> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    ///
    /// ControlInfoSource initializes with data read from a static json file included in the
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class ControlInfoDataSource
    {
        private static readonly object _lock = new object();

        #region Singleton

        private static ControlInfoDataSource _instance;

        public static ControlInfoDataSource Instance
        {
            get
            {
                return _instance;
            }
        }

        static ControlInfoDataSource()
        {
            _instance = new ControlInfoDataSource();
        }

        private ControlInfoDataSource() { }

        #endregion

        private IList<ControlInfoDataGroup> _groups = new List<ControlInfoDataGroup>();
        public IList<ControlInfoDataGroup> Groups
        {
            get { return this._groups; }
        }

        public async Task<IEnumerable<ControlInfoDataGroup>> GetGroupsAsync()
        {
            await _instance.GetControlInfoDataAsync();

            return _instance.Groups;
        }

        public async Task<ControlInfoDataGroup> GetGroupAsync(string uniqueId)
        {
            await _instance.GetControlInfoDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _instance.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public async Task<ControlInfoDataItem> GetItemAsync(string uniqueId)
        {
            await _instance.GetControlInfoDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _instance.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() > 0) return matches.First();
            return null;
        }

        public async Task<ControlInfoDataGroup> GetGroupFromItemAsync(string uniqueId)
        {
            await _instance.GetControlInfoDataAsync();
            var matches = _instance.Groups.Where((group) => group.Items.FirstOrDefault(item => item.UniqueId.Equals(uniqueId)) != null);
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetControlInfoDataAsync()
        {
            lock (_lock)
            {
                if (this.Groups.Count() != 0)
                {
                    return;
                }
            }

            //Uri dataUri = new Uri("ms-appx:///DataModel/ControlInfoData.json");
            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);

            //string jsonText = await FileIO.ReadTextAsync(file);

            JObject jsonObject = JObject.Parse(jsonFile);


            JArray jsonArray = (JArray) jsonObject["Groups"];

            lock (_lock)
            {
                foreach (JObject groupValue in jsonArray)
                {
                    var groupObject = groupValue;
                    ControlInfoDataGroup group = new ControlInfoDataGroup((string) groupObject["UniqueId"],
                                                                          (string)groupObject["Title"],
                                                                          (string) groupObject["Subtitle"],
                                                                          (string) groupObject["ImagePath"],
                                                                          (string) groupObject["Description"]);

                    foreach (JObject itemValue in (JArray) groupObject["Items"])
                    {
                        var itemObject = itemValue;

                        string badgeString = null;

                        bool isNew = itemObject.ContainsKey("IsNew") ? (bool) itemObject["IsNew"] : false;
                        bool isUpdated = itemObject.ContainsKey("IsUpdated") ? (bool) itemObject["IsUpdated"]: false;
                        bool isPreview = itemObject.ContainsKey("IsPreview") ? (bool) itemObject["IsPreview"] : false;

                        if (isNew)
                        {
                            badgeString = "New";
                        }
                        else if (isUpdated)
                        {
                            badgeString = "Updated";
                        }
                        else if (isPreview)
                        {
                            badgeString = "Preview";
                        }

                        var item = new ControlInfoDataItem((string) itemObject["UniqueId"],
                                                                (string) itemObject["Title"],
                                                                (string) itemObject["Subtitle"],
                                                                (string) itemObject["ImagePath"],
                                                                badgeString,
                                                                (string) itemObject["Description"],
                                                                (string) itemObject["Content"],
                                                                isNew,
                                                                isUpdated,
                                                                isPreview);

                        if (itemObject.ContainsKey("Docs"))
                        {
                            foreach (JObject docValue in (JArray) itemObject["Docs"])
                            {
                                var docObject = docValue;
                                item.Docs.Add(new ControlInfoDocLink((string)docObject["Title"], (string) docObject["Uri"]));
                            }
                        }

                        if (itemObject.ContainsKey("RelatedControls"))
                        {
                            foreach (var relatedControlValue in (JArray) itemObject["RelatedControls"])
                            {
                                item.RelatedControls.Add((string)relatedControlValue);
                            }
                        }

                        group.Items.Add(item);
                    }

                    if (!Groups.Any(g => g.Title == group.Title))
                    {
                        Groups.Add(group);
                    }
                }
            }
        }


        private readonly string jsonFile = @"{
  'Groups': [
    {
      'UniqueId': 'NewControls',
      'Title': 'What's New',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': []
    },
    {
      'UniqueId': 'AllControls',
      'Title': 'All controls',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': []
    },
    {
      'UniqueId': 'MenusAndToolbars',
      'Title': 'Menus and Toolbars',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'XamlUICommand',
          'Title': 'XamlUICommand',
          'Subtitle': 'An object which is used to define the look and feel of a given command.',
          'ImagePath': 'ms-appx:///Assets/AppBarSeparator.png',
          'Description': 'An object which is used to define the look and feel of a given command, which can be reused across your app, and which is understood natively by the standard XAML controls.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/commanding#command-experiences-using-the-xamluicommand-class'
            },
            {
              'Title': 'XamlUICommand - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.input.xamluicommand'
            }
          ],
          'RelatedControls': [
            'StandardUICommand',
            'AppBarButton',
            'AppBarToggleButton',
            'CommandBar'
          ]
        },
        {
          'UniqueId': 'StandardUICommand',
          'Title': 'StandardUICommand',
          'Subtitle': 'A StandardUICommand is a built-in 'XamlUICommand' which represents a commonly used command, e.g. 'Save'.',
          'ImagePath': 'ms-appx:///Assets/AppBarSeparator.png',
          'Description': 'StandardUICommands are a set of built-in XamlUICommands represeting commonly used commands. Including the look and feel of a given command, which can be reused across your app, and which is understood natively by the standard XAML controls. E.g. Save, Open, Copy, Paste, etc.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/commanding#command-experiences-using-the-standarduicommand-class'
            },
            {
              'Title': 'StandardUICommand - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.input.standarduicommand'
            }
          ],
          'RelatedControls': [
            'XamlUICommand',
            'AppBarButton',
            'AppBarToggleButton',
            'CommandBar'
          ]
        },
        {
          'UniqueId': 'AppBarButton',
          'Title': 'AppBarButton',
          'Subtitle': 'A button that's styled for use in a CommandBar.',
          'ImagePath': 'ms-appx:///Assets/AppBarButton.png',
          'Description': 'App bar buttons differ from standard buttons in several ways:\n- Their default appearance is a transparent background with a smaller size.\n- You use the Label and Icon properties to set the content instead of the Content property. The Content property is ignored.\n- The button's IsCompact property controls its size.',
          'Content': '<p>You can open the app bar and toggle the IsCompact button to see how the app bar buttons on this page change.</p><p>Use the <b>Label</b> and <b>Icon</b> properties to define the content of the app bar buttons. Set the <b>Label</b> property to a string to specify the text label. The label is shown by default but is hidden when the button is in its compact state, so you also need to specify a meaningful icon. To do that, set the button's <b>Icon</b> property to an element derived from the <b>IconElement</b> class. Four kinds of icon elements are provided:</p><p><b>FontIcon</b> - The icon is based on a glyph from the specified font family.</p><p><b>BitmapIcon</b> - The icon is based on a bitmap image file with the specified Uri.</p><p><b>PathIcon</b> - The icon is based on Path data.</p><p><b>SymbolIcon</b> - The icon is based on a predefined list of glyphs from the Segoe UI Symbol font.</p><p>Look at the <i>AppBarButtonPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'AppBarButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.appbarbutton'
            },
            {
              'Title': 'SymbolIcon - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.symbolicon'
            },
            {
              'Title': 'FontIcon - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.fonticon'
            },
            {
              'Title': 'BitmapIcon - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.bitmapicon'
            },
            {
              'Title': 'PathIcon - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.pathicon'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/controls-and-patterns/app-bars'
            }
          ],
          'RelatedControls': [ 'AppBarToggleButton', 'AppBarSeparator', 'CommandBar' ]
        },
        {
          'UniqueId': 'AppBarSeparator',
          'Title': 'AppBarSeparator',
          'Subtitle': 'A vertical line that's used to visually separate groups of commands in an app bar.',
          'ImagePath': 'ms-appx:///Assets/AppBarSeparator.png',
          'Description': 'An AppBarSeparator creates a vertical line to visually separate groups of commands in a app bar. It has a compact state with reduced padding to match the compact state of the AppBarButton and AppBarToggleButton controls.',
          'Content': '<p>You can open the app bar and toggle the IsCompact button to see how the app bar buttons and separators on this page change.</p><p>When the <b>IsCompact</b> property is true, the padding around the <b>AppBarSeparator</b> is reduced.</p><p>Look at the <i>AppBarSeparatorPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': false,
          'Docs': [
            {
              'Title': 'AppBarSeparator - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.appbarseparator'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/controls-and-patterns/app-bars'
            }
          ],
          'RelatedControls': [
            'AppBarButton',
            'AppBarToggleButton',
            'CommandBar'
          ]
        },
        {
          'UniqueId': 'AppBarToggleButton',
          'Title': 'AppBarToggleButton',
          'Subtitle': 'A button that can be on, off, or indeterminate like a CheckBox, and is styled for use in an app bar or other specialized UI.',
          'ImagePath': 'ms-appx:///Assets/AppBarToggleButton.png',
          'Description': 'An AppBarToggleButton looks like an AppBarButton, but works like a CheckBox. It typically has two states, checked (on) or unchecked (off), but can be indeterminate if the IsThreeState property is true. You can determine it's state by checking the IsChecked property.',
          'Content': '<p>You can open the app bar and toggle the IsCompact button to see how the app bar buttons on this page change.</p><p>Look at the <i>AppBarToggleButtonPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'AppBarToggleButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.appbartogglebutton'
            },
            {
              'Title': 'SymbolIcon - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.symbolicon'
            },
            {
              'Title': 'FontIcon - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.fonticon'
            },
            {
              'Title': 'BitmapIcon - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.bitmapicon'
            },
            {
              'Title': 'PathIcon - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.pathicon'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/controls-and-patterns/app-bars'
            }
          ],
          'RelatedControls': [
            'AppBarButton',
            'AppBarSeparator',
            'CommandBar'
          ]
        },
        {
          'UniqueId': 'CommandBar',
          'Title': 'CommandBar',
          'Subtitle': 'A toolbar for displaying application-specific commands that handles layout and resizing of its contents.',
          'ImagePath': 'ms-appx:///Assets/CommandBar.png',
          'Description': 'The CommandBar simplifies the creation of basic app bars by providing:\n- Automatic layout of commands, with primary commands on the right and secondary commands on the left.\n- Automatic resizing of app bar commands when the app size changes.\nWhen you need an app bar that contains only AppBarButton,AppBarToggleButton , and AppBarSeparator controls, use a CommandBar. If you need more complex content, such as images, progress bars, or text blocks, use an AppBar control.',
          'Content': '<p>The bottom app bar on this page is a <b>CommandBar</b> control.</p><p>Add secondary commands and then resize the app to see how the <b>CommandBar</b> automatically adapts to different widths.</p><p>This <b>CommandBar</b> element is in the ItemPage so it can be shared across all control pages in the app. Look at the <i>ItemPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': false,
          'Docs': [
            {
              'Title': 'CommandBar - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.commandbar'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/controls-and-patterns/app-bars'
            }
          ],
          'RelatedControls': [
            'AppBarButton',
            'AppBarToggleButton',
            'AppBarSeparator'
          ]
        },
        {
          'UniqueId': 'MenuBar',
          'Title': 'MenuBar',
          'Subtitle': 'A classic menu, allowing the display of MenuItems containing MenuFlyoutItems.',
          'ImagePath': 'ms-appx:///Assets/MenuBar.png',
          'Description': 'The Menubar simplifies the creation of basic applications by providing a set of menus at the top of the app or window.',
          'Content': '',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'MenuBar - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.MenuBar'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/menus'
            }
          ],
          'RelatedControls': [
            'CommandBar',
            'MenuFlyout',
            'StandardUICommand',
            'XamlUICommand'
          ]
        },
        {
          'UniqueId': 'CommandBarFlyout',
          'Title': 'CommandBarFlyout',
          'Subtitle': 'A mini-toolbar displaying proactive commands, and an optional menu of commands.',
          'ImagePath': 'ms-appx:///Assets/CommandBarFlyout.png',
          'Description': 'A mini-toolbar which displays a set of proactive commands, as well as a secondary menu of commands if desired.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'CommandBarFlyout - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.commandbarflyout'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/command-bar-flyout'
            }
          ],
          'RelatedControls': [
            'CommandBar',
            'MenuFlyout',
            'RichEditBox',
            'TextBox',
            'StandardUICommand',
            'XamlUICommand'
          ]
        },
        {
          'UniqueId': 'MenuFlyout',
          'Title': 'MenuFlyout',
          'Subtitle': 'Shows a contextual list of simple commands or options.',
          'ImagePath': 'ms-appx:///Assets/MenuFlyout.png',
          'Description': 'A MenuFlyout displays lightweight UI that is light dismissed by clicking or tapping off of it. Use it to let the user choose from a contextual list of simple commands or options.',
          'Content': '<p>Look at the <i>MenuFlyoutPage.xaml</i> file in Visual Studio to see the full code.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'MenuFlyout - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.menuflyout'
            },
            {
              'Title': 'MenuFlyoutItem - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.menuflyoutitem'
            },
            {
              'Title': 'MenuFlyoutSeparator - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.menuflyoutseparator'
            },
            {
              'Title': 'ToggleMenuFlyoutItem - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.togglemenuflyoutitem'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/menus'
            }
          ],
          'RelatedControls': [
            'Flyout',
            'ContentDialog',
            'Button',
            'AppBarButton'
          ]
        },
        {
          'UniqueId': 'SwipeControl',
          'Title': 'SwipeControl',
          'Subtitle': 'Touch gesture for quick menu actions on items.',
          'ImagePath': 'ms-appx:///Assets/Swipe.png',
          'Description': 'Touch gesture for quick menu actions on items.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'SwipeControl - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.swipecontrol'
            },
            {
              'Title': 'SwipeItems - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.swipeitems'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/swipe'
            },
            {
              'Title': 'Gesture Actions',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/collection-commanding/'
            }

          ],
          'RelatedControls': [
            'GridView',
            'ListView'
          ]
        }
      ]
    },
    {
      'UniqueId': 'Collections',
      'Title': 'Collections',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'FlipView',
          'Title': 'FlipView',
          'Subtitle': 'Presents a collection of items that the user can flip through, one item at a time.',
          'ImagePath': 'ms-appx:///Assets/FlipView.png',
          'Description': 'The FlipView lets you flip through a collection of items, one at a time. It's great for displaying images from a gallery, pages of a magazine, or similar items.',
          'Content': '<p><b>FlipView</b> is an <b>ItemsControl</b>, so it can contain a collection of items of any type. To populate the view, add items to the <b>Items</b> collection, or set the <b>ItemsSource</b> property to a data source.</p><p>Look at the <i>FlipViewPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'FlipView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.flipview'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/flipview'
            }
          ],
          'RelatedControls': [
            'GridView',
            'ListView',
            'SemanticZoom'
          ]
        },
        {
          'UniqueId': 'GridView',
          'Title': 'GridView',
          'Subtitle': 'A control that presents a collection of items in rows and columns.',
          'ImagePath': 'ms-appx:///Assets/GridView.png',
          'Description': 'The GridView lets you show a collection of items arranged in rows and columns that scroll horizontally.',
          'Content': '<p><b>GridView</b> is an <b>ItemsControl</b>, so it can contain a collection of items of any type. To populate the view, add items to the <b>Items</b> collection, or set the <b>ItemsSource</b> property to a data source.</p><p>Set an <b>ItemTemplate</b> to define the look of individual items.</p><p>Look at the <i>GridViewPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'GridView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.gridview'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/lists'
            }
          ],
          'RelatedControls': [
            'ListView',
            'FlipView',
            'SemanticZoom'
          ]
        },
        {
          'UniqueId': 'ListBox',
          'Title': 'ListBox',
          'Subtitle': 'A control that presents an inline list of items that the user can select from.',
          'ImagePath': 'ms-appx:///Assets/ListBox.png',
          'Description': 'Use a ListBox when you want the options to be visible all the time or when users can select more than one option at a time. ListBox controls are always open, which allows several items to be displayed to the user without user interaction.',
          'Content': '<p>Look at the <i>ListBoxPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'ListBox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.listbox'
            },
            {
              'Title': 'ListBoxItem - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.listboxitem'
            }
          ],
          'RelatedControls': [
            'ComboBox',
            'RadioButton',
            'CheckBox',
            'AutoSuggestBox'
          ]
        },
        {
          'UniqueId': 'ListView',
          'Title': 'ListView',
          'Subtitle': 'A control that presents a collection of items in a vertical list.',
          'ImagePath': 'ms-appx:///Assets/ListView.png',
          'Description': 'The ListView lets you show a collection of items in a list that scrolls vertically.',
          'Content': '<p><b>ListView</b> is an <b>ItemsControl</b>, so it can contain a collection of items of any type. To populate the view, add items to the <b>Items</b> collection, or set the <b>ItemsSource</b> property to a data source.</p><p>Set an <b>ItemTemplate</b> to define the look of individual items.</p><p>Look at the <i>ListViewPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'ListView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.listview'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/listview-and-gridview'
            },
            {
              'Title': 'Drag and Drop - Full Sample',
              'Uri': 'https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/XamlDragAndDrop'
            },
            {
              'Title': 'CollectionViewSource - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Data.CollectionViewSource#see-also'
            },
            {
              'Title': 'Filtering collections and lists through user input',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/listview-filtering'
            },
            {
              'Title': 'Inverted Lists',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/inverted-lists'
            },
            {
              'Title': 'Inverted Lists - Full Sample',
              'Uri': 'https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/XamlBottomUpList'
            }

          ],
          'RelatedControls': [
            'GridView',
            'StandardUICommand',
            'FlipView',
            'SemanticZoom'
          ]
        },
        {
          'UniqueId': 'PullToRefresh',
          'Title': 'PullToRefresh',
          'Subtitle': 'Provides the ability to pull on a collection of items in a list/grid to refresh the contents of the collection.',
          'ImagePath': 'ms-appx:///Assets/PullToRefresh.png',
          'Description': 'PullToRefresh Provides the ability to pull on a collection of items in a list/grid to refresh the contents of the collection.',
          'Content': '<p><b>PullToRefresh</b> can be used for a collection of items of any type. To populate the view, add items to the <b>Items</b> collection, or set the <b>ItemsSource</b> property to a data source.</p><p>Set an <b>ItemTemplate</b> to define the look of individual items.</p><p>Look at the <i>ListViewPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'RefreshContainer - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.RefreshContainer'
            },
            {

              'Title': 'RefreshVisualizer - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.RefreshVisualizer'
            },
            {

              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/pull-to-refresh'
            }
          ]
        },
        {
          'UniqueId': 'TreeView',
          'Title': 'TreeView',
          'Subtitle': 'The  TreeView control is a hierarchical list pattern with expanding and collapsing nodes that contain nested items.',
          'ImagePath': 'ms-appx:///Assets/TreeView.png',
          'Description': 'The TreeView control is a hierarchical list pattern with expanding and collapsing nodes that contain nested items. ',
          'Content': '<p><b>PullToRefresh</b> can be used for a collection of items of any type. To populate the view, add items to the <b>Items</b> collection, or set the <b>ItemsSource</b> property to a data source.</p><p>Set an <b>ItemTemplate</b> to define the look of individual items.</p><p>Look at the <i>ListViewPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': false,
          'Docs': [
            {
              'Title': 'TreeView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.TreeView'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/tree-view'
            }
          ]
        },
        {
          'UniqueId': 'DataGrid',
          'Title': 'DataGrid',
          'Subtitle': 'The DataGrid control presents data in a customizable table of rows and columns.',
          'ImagePath': 'ms-appx:///Assets/GridView.png',
          'Description': 'The DataGrid control provides a flexible way to display a collection of data in rows and columns.',
          'Content': 'The DataGrid control presents data in a customizable table of rows and columns.',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'DataGrid - API',
              'Uri': 'https://aka.ms/win10datagridapidoc'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://aka.ms/win10datagriddoc'
            },
            {
              'Title': 'Windows Community Toolkit',
              'Uri': 'https://docs.microsoft.com/windows/communitytoolkit/'
            }
          ],
          'RelatedControls': [
            'GridView',
            'TreeView'
          ]
        }
      ]
    },
    {
      'UniqueId': 'DateAndTime',
      'Title': 'Date and Time',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'CalendarDatePicker',
          'Title': 'CalendarDatePicker',
          'Subtitle': 'A control that lets users pick a date value using a calendar.',
          'ImagePath': 'ms-appx:///Assets/CalenderDatePicker.png',
          'Description': 'A control that lets users pick a date value using a calendar.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'CalendarDatePicker - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.calendardatepicker'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/calendar-date-picker'
            }
          ],
          'RelatedControls': [
            'DatePicker',
            'CalendarView'
          ]
        },
        {
          'UniqueId': 'CalendarView',
          'Title': 'CalendarView',
          'Subtitle': 'A control that presents a calendar for a user to choose a date from.',
          'ImagePath': 'ms-appx:///Assets/CalendarView.png',
          'Description': 'CalendarView shows a larger view for showing and selecting dates.  DatePicker by contrast has a compact view with inline selection.',
          'Content': '<p>Look at the <i>CalendarView.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'CalendarView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.calendarview'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/calendar-view'
            }
          ],
          'RelatedControls': [
            'CalendarDatePicker',
            'DatePicker',
            'TimePicker'
          ]
        },
        {
          'UniqueId': 'DatePicker',
          'Title': 'DatePicker',
          'Subtitle': 'A control that lets a user pick a date value.',
          'ImagePath': 'ms-appx:///Assets/DatePicker.png',
          'Description': 'Use a DatePicker to let users set a date in your app, for example to schedule an appointment. The DatePicker displays three controls for month, date, and year. These controls are easy to use with touch or mouse, and they can be styled and configured in several different ways.',
          'Content': '<p>Look at the <i>DatePickerPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'DatePicker - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.datepicker'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/date-picker'
            }
          ],
          'RelatedControls': [
            'CalendarDatePicker',
            'CalendarView',
            'TimePicker'
          ]
        },
        {
          'UniqueId': 'TimePicker',
          'Title': 'TimePicker',
          'Subtitle': 'A configurable control that lets a user pick a time value.',
          'ImagePath': 'ms-appx:///Assets/TimePicker.png',
          'Description': 'Use a TimePicker to let users set a time in your app, for example to set a reminder. The TimePicker displays three controls for month, day, and and AM/PM. These controls are easy to use with touch or mouse, and they can be styled and configured in several different ways.',
          'Content': '<p>Look at the <i>TimePickerPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'TimePicker - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.timepicker'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/time-picker'
            }
          ],
          'RelatedControls': [
            'DatePicker',
            'CalendarView'
          ]
        }
      ]
    },
    {
      'UniqueId': 'BasicInput',
      'Title': 'Basic Input',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'Button',
          'Title': 'Button',
          'Subtitle': 'A control that responds to user input and raises a Click event.',
          'ImagePath': 'ms-appx:///Assets/Button.png',
          'Description': 'The Button control provides a Click event to respond to user input from a touch, mouse, keyboard, stylus, or other input device. You can put different kinds of content in a button, such as text or an image, or you can restyle a button to give it a new look.',
          'Content': '<p>The main purpose of a <b>Button</b> is to make something happen when a user clicks it. There are two ways you can make something happen:</p><ul><li>Handle the <b>Click</b> event.</li><li>Bind the <b>Command</b> property to an ICommand implementation that describes the command logic.</li></ul><p>Buttons often have only simple string content, but you can use any object as content. You can also change the style and template to give them any look you want.</p><p>Look at the <i>ButtonPage.xaml</i> file in Visual Studio to see the custom button style and template definitions used on this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'Button - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.button'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/buttons'
            }
          ],
          'RelatedControls': [
            'ToggleButton',
            'RepeatButton',
            'HyperlinkButton',
            'AppBarButton'
          ]
        },
        {
          'UniqueId': 'DropDownButton',
          'Title': 'DropDownButton',
          'Subtitle': 'A button that displays a flyout of choices when clicked.',
          'ImagePath': 'ms-appx:///Assets/DropDownButton.png',
          'Description': 'A control that drops down a flyout of choices from which one can be chosen.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'DropDownButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.dropdownbutton'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/buttons'
            }
          ],
          'RelatedControls': [
            'SplitButton',
            'ToggleSplitButton',
            'ToggleButton',
            'RepeatButton',
            'HyperlinkButton',
            'AppBarButton',
            'CommandBar'
          ]
        },
        {
          'UniqueId': 'HyperlinkButton',
          'Title': 'HyperlinkButton',
          'Subtitle': 'A button that appears as hyperlink text, and can navigate to a URI or handle a Click event.',
          'ImagePath': 'ms-appx:///Assets/HyperlinkButton.png',
          'Description': 'A HyperlinkButton appears as a text hyperlink. When a user clicks it, it opens the page you specify in the NavigateUri property in the default browser. Or you can handle its Click event, typically to navigate within your app.',
          'Content': '<p>A <b>HyperlinkButton</b> looks like hyperlink text, but works like a button. You can use it in two ways:</p><ul><li>Set the <b>NavigateUri</b> property. When a user clicks it, it will automatically open the URI in the default browser.</li><li>Handle the <b>Click</b> event. This works just like the <b>Click</b> event of a standard <b>Button</b>, and can be used to navigate within your app.</li></ul><p>Each control page in this app has two sets of hyperlink buttons, one set to open documentation in Internet Explorer, and one set to navigate to related control pages in the app.</p><p>Look at the <i>HyperlinkButtonPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'HyperlinkButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.hyperlinkbutton'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/hyperlinks'
            }
          ],
          'RelatedControls': [
            'Button',
            'ToggleButton',
            'RepeatButton',
            'AppBarButton'
          ]
        },
        {
          'UniqueId': 'RepeatButton',
          'Title': 'RepeatButton',
          'Subtitle': 'A button that raises its Click event repeatedly from the time it's pressed until it's released.',
          'ImagePath': 'ms-appx:///Assets/Button.png',
          'Description': 'The RepeatButton control is like a standard Button, except that the Click event occurs continuously while the user presses the RepeatButton.',
          'Content': '<p>A <b>RepeatButton</b> looks just like a regular <b>Button</b>, but it's <b>Click</b> event occurs continuously while the button is pressed.</p><p>Look at the <i>RepeatButtonPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'RepeatButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.primitives.repeatbutton'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/buttons'
            }
          ],
          'RelatedControls': [
            'Button',
            'ToggleButton',
            'HyperlinkButton',
            'AppBarButton'
          ]
        },
        {
          'UniqueId': 'ToggleButton',
          'Title': 'ToggleButton',
          'Subtitle': 'A button that can be switched between two states like a CheckBox.',
          'ImagePath': 'ms-appx:///Assets/ToggleButton.png',
          'Description': 'A ToggleButton looks like a Button, but works like a CheckBox. It typically has two states, checked (on) or unchecked (off), but can be indeterminate if the IsThreeState property is true. You can determine it's state by checking the IsChecked property.',
          'Content': '<p><b>ToggleButton</b> is used as a base class for similar controls like <b>CheckBox</b> and <b>RadioButton</b>. It can be used on its own, but don't use it if a <b>CheckBox</b>, <b>RadioButton</b>, or <b>ToggleSwitch</b> would convey your intent better.</p><p>Look at the <i>ToggleButtonPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'ToggleButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.primitives.togglebutton'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/buttons#create-a-toggle-split-button'
            }
          ],
          'RelatedControls': [
            'Button',
            'AppBarToggleButton',
            'ToggleSwitch',
            'CheckBox',
            'CommandBarFlyout',
            'CommandBar'
          ]
        },
        {
          'UniqueId': 'SplitButton',
          'Title': 'SplitButton',
          'Subtitle': 'A two-part button that displays a flyout when its secondary part is clicked.',
          'ImagePath': 'ms-appx:///Assets/SplitButton.png',
          'Description': 'The splitbutton is a dropdown button, but with an addition execution hit target.',
          'Content': '',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'SplitButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.splitbutton'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/buttons#create-a-split-button'
            }
          ],
          'RelatedControls': [
            'Button',
            'AppBarToggleButton',
            'CommandBar',
            'ToggleSwitch',
            'CheckBox',
            'CommandBarFlyout',
            'CommandBar'
          ]
        },
        {
          'UniqueId': 'ToggleSplitButton',
          'Title': 'ToggleSplitButton',
          'Subtitle': 'A version of the SplitButton where the activation target toggles on/off.',
          'ImagePath': 'ms-appx:///Assets/ToggleSplitButton.png',
          'Description': 'A version of the SplitButton where the activation target toggles on/off.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'ToggleSplitButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.togglesplitbutton'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/buttons'
            }
          ],
          'RelatedControls': [
            'Button',
            'AppBarToggleButton',
            'ToggleSwitch',
            'CheckBox',
            'CommandBarFlyout',
            'CommandBar'
          ]
        },
        {
          'UniqueId': 'CheckBox',
          'Title': 'CheckBox',
          'Subtitle': 'A control that a user can select or clear.',
          'ImagePath': 'ms-appx:///Assets/CheckBox.png',
          'Description': 'CheckBox controls let the user select a combination of binary options. In contrast, RadioButton controls allow the user to select from mutually exclusive options. The indeterminate state is used to indicate that an option is set for some, but not all, child options. Don't allow users to set an indeterminate state directly to indicate a third option.',
          'Content': '<p>Check and uncheck these controls to see how they look in each state. The label for each <b>CheckBox</b> is defined by its <b>Content</b> property.</p><p>Use a three-state <b>CheckBox</b> to show that none, some, or all of an items sub-options are checked. You have to add some code to do this. Take a look at the methods in the <i>SelectAllMethods</i> region of <i>CheckBoxPage.xaml.cs</i> to see how we did it.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'CheckBox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.checkbox'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/checkbox'
            }
          ],
          'RelatedControls': [
            'RadioButton',
            'ToggleSwitch',
            'ToggleButton'
          ]
        },
        {
          'UniqueId': 'ColorPicker',
          'Title': 'ColorPicker',
          'Subtitle': 'A control that displays a selectable color spectrum.',
          'ImagePath': 'ms-appx:///Assets/ColorPicker.png',
          'Description': 'A selectable color spectrum.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'ColorPicker - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.ColorPicker'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/color-picker'
            }
          ],
          'RelatedControls': [
            'ComboBox'
          ]
        },
        {
          'UniqueId': 'ComboBox',
          'Title': 'ComboBox',
          'Subtitle': 'A drop-down list of items a user can select from.',
          'ImagePath': 'ms-appx:///Assets/ComboBox.png',
          'Description': 'Use a ComboBox when you need to conserve on-screen space and when users select only one option at a time. A ComboBox shows only the currently selected item.',
          'Content': '',
          'IsUpdated': false,
          'Docs': [
            {
              'Title': 'ComboBox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.combobox'
            },
            {
              'Title': 'ComboBoxItem - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.comboboxitem'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/combo-box'
            }
          ],
          'RelatedControls': [
            'ListBox',
            'RadioButton',
            'CheckBox',
            'ListView',
            'AutoSuggestBox',
            'RatingControl'
          ]
        },
        {
          'UniqueId': 'RadioButton',
          'Title': 'RadioButton',
          'Subtitle': 'A control that allows a user to select a single option from a group of options.',
          'ImagePath': 'ms-appx:///Assets/RadioButton.png',
          'Description': 'Use RadioButtons to let a user choose between mutually exclusive, related options. Generally contained within a RadioButtons group control.',
          'Content': '<p>Look at the <i>RadioButtonPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'RadioButton - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.radiobutton'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/radio-button'
            }
          ],
          'RelatedControls': [
            'CheckBox',
            'RadioButtons',
            'ToggleSwitch',
            'ToggleButton'
          ]
        },
        {
          'UniqueId': 'RatingControl',
          'Title': 'RatingControl',
          'Subtitle': 'Rate something 1 to 5 stars.',
          'ImagePath': 'ms-appx:///Assets/RatingControl.png',
          'Description': 'Rate something 1 to 5 stars.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'RatingControl - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.RatingControl'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/rating'
            }
          ],
          'RelatedControls': [
            'Slider',
            'ComboBox'
          ]
        },
        {
          'UniqueId': 'Slider',
          'Title': 'Slider',
          'Subtitle': 'A control that lets the user select from a range of values by moving a Thumb control along a track.',
          'ImagePath': 'ms-appx:///Assets/Slider.png',
          'Description': 'Use a Slider when you want your users to be able to set defined, contiguous values (such as volume or brightness) or a range of discrete values (such as screen resolution settings).',
          'Content': '<p>Look at the <i>SliderPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Slider - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.slider'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'http://docs.microsoft.com/windows/uwp/design/controls-and-patterns/slider'
            }
          ],
          'RelatedControls': [
            'ComboBox',
            'ListBox',
            'RatingControl'
          ]
        },
        {
          'UniqueId': 'ToggleSwitch',
          'Title': 'ToggleSwitch',
          'Subtitle': 'A switch that can be toggled between 2 states.',
          'ImagePath': 'ms-appx:///Assets/ToggleSwitch.png',
          'Description': 'Use ToggleSwitch controls to present users with exactly two mutually exclusive options (like on/off), where choosing an option results in an immediate commit. A toggle switch should have a single label.',
          'Content': '<p>Look at the <i>ToggleSwitchPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'ToggleSwitch - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.toggleswitch'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/toggles'
            }
          ],
          'RelatedControls': [
            'ToggleButton',
            'RadioButton',
            'CheckBox',
            'AppBarToggleButton'
          ]
        }
      ]
    },
    {
      'UniqueId': 'StatusAndInfo',
      'Title': 'Status and Info',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'AutomationProperties',
          'Title': 'AutomationProperties',
          'Subtitle': 'Attributes that help with writing accessible XAML.',
          'ImagePath': 'ms-appx:///Assets/Button.png',
          'Description': 'The AutomationProperties attributes provide functionality around accessible components.',
          'Content': '<p>The aim of <b>AutomationProperties</b> is to enable components that are accessible to screen readers.',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'AutomationProperties - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.automation.automationproperties'
            },
            {
              'Title': 'Accessibility Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/accessibility/accessibility'
            }
          ],
          'RelatedControls': [
            'TextBlock',
            'Image'
          ]
        },
        {
          'UniqueId': 'ProgressBar',
          'Title': 'ProgressBar',
          'Subtitle': 'Shows the apps progress on a task, or that the app is performing ongoing work that doesn't block user interaction.',
          'ImagePath': 'ms-appx:///Assets/ProgressBar.png',
          'Description': 'The ProgressBar has two different visual representations:\nIndeterminate - shows that a task is ongoing, but doesn't block user interaction.\nDeterminate - shows how much progress has been made on a known amount of work.',
          'Content': '<p>Look at the <i>ProgressBarPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'ProgressBar - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Microsoft.UI.Xaml.Controls.ProgressBar'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/progress-controls'
            }
          ],
          'RelatedControls': [
            'ProgressRing'
          ]
        },
        {
          'UniqueId': 'ProgressRing',
          'Title': 'ProgressRing',
          'Subtitle': 'Shows that the app is performing ongoing work that blocks user interaction.',
          'ImagePath': 'ms-appx:///Assets/ProgressRing.png',
          'Description': 'Use a ProgressRing to show that the app is performing ongoing work that blocks user interaction.',
          'Content': '<p>Look at the <i>ProgressRingPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'ProgressRing - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.progressring'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/progress-controls'
            }
          ],
          'RelatedControls': [
            'ProgressBar'
          ]
        },
        {
          'UniqueId': 'ToolTip',
          'Title': 'ToolTip',
          'Subtitle': 'Displays information for an element in a pop-up window.',
          'ImagePath': 'ms-appx:///Assets/ToolTip.png',
          'Description': 'A ToolTip shows more information about a UI element. You might show information about what the element does, or what the user should do. The ToolTip is shown when a user hovers over or presses and holds the UI element.',
          'Content': '<p>Look at the <i>ToolTipPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'ToolTip - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.tooltip'
            },
            {
              'Title': 'Guidance',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/tooltips'
            }
          ],
          'RelatedControls': [
            'TeachingTip',
            'Flyout',
            'ContentDialog'
          ]
        }
      ]
    },
    {
      'UniqueId': 'DialogsAndFlyouts',
      'Title': 'Dialogs and Flyouts',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'ContentDialog',
          'Title': 'ContentDialog',
          'Subtitle': 'A dialog box that can be customized to contain any XAML content.',
          'ImagePath': 'ms-appx:///Assets/DefaultIcon.png',
          'Description': 'Use a ContentDialog to show relavant information or to provide a modal dialog experience that can show any XAML content.',
          'Content': '<p>Look at the <i>ContentDialog.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'ContentDialog - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.contentdialog'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/dialogs-and-flyouts/dialogs'
            }
          ],
          'RelatedControls': [
            'Flyout',
            'MenuFlyout',
            'TeachingTip',
            'ToolTip'
          ]
        },
        {
          'UniqueId': 'Flyout',
          'Title': 'Flyout',
          'Subtitle': 'Shows contextual information and enables user interaction.',
          'ImagePath': 'ms-appx:///Assets/Flyout.png',
          'Description': 'A Flyout displays lightweight UI that is either information, or requires user interaction. Unlike a dialog, a Flyout can be light dismissed by clicking or tapping off of it. Use it to collect input from the user, show more details about an item, or ask the user to confirm an action.',
          'Content': '<p>Look at the <i>FlyoutPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Flyout - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.flyout'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/dialogs'
            }
          ],
          'RelatedControls': [
            'TeachingTip',
            'ContentDialog',
            'MenuFlyout',
            'Button',
            'AppBarButton'
          ]
        },
        {
          'UniqueId': 'TeachingTip',
          'Title': 'TeachingTip',
          'Subtitle': 'A content-rich flyout for guiding users and enabling teaching moments.',
          'ImagePath': 'ms-appx:///Assets/TeachingTip.png',
          'Description': 'The XAML TeachingTip Control provides a way for your app to guide and inform users in your application with a non-invasive and content rich notification. TeachingTip can be used for bringing focus to a new or important feature, teaching users how to perform a task, or enhancing the user workflow by providing contextually relevant information to their task at hand.',
          'Content': '<p>Look at the <i>TeachingTip.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'TeachingTip - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.teachingtip'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/dialogs-and-flyouts/teaching-tip'
            }
          ],
          'RelatedControls': [
            'ContentDialog',
            'Flyout',
            'ToolTip'
          ]
        }
      ]
    },
    {
      'UniqueId': 'Scrolling',
      'Title': 'Scrolling',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'ScrollViewer',
          'Title': 'ScrollViewer',
          'Subtitle': 'A container control that lets the user pan and zoom its content.',
          'ImagePath': 'ms-appx:///Assets/ScrollViewer.png',
          'Description': 'A ScrollViewer lets a user scroll, pan, and zoom to see content that's larger than the viewable area. Many content controls, like ListView, have ScrollViewers built into their control templates to provide automatic scrolling.',
          'Content': '<p>Look at the <i>ScrollViewerPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'ScrollViewer - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.scrollviewer'
            },
            {
              'Title': 'Guidance',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/scroll-controls'
            }
          ],
          'RelatedControls': [
            'ViewBox',
            'Canvas',
            'Grid',
            'StackPanel',
            'RelativePanel',
            'ParallaxView'
          ]
        },
        {
          'UniqueId': 'SemanticZoom',
          'Title': 'SemanticZoom',
          'Subtitle': 'Lets the user zoom between two different views of a collection, making it easier to navigate through large collections of items.',
          'ImagePath': 'ms-appx:///Assets/DefaultIcon.png',
          'Description': 'The SemanticZoom lets you show grouped data in two different ways, and is useful for quickly navigating through large sets of data.',
          'Content': '<p>Look at the <i>SemanticZoomPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'SemanticZoom - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.semanticzoom'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/semantic-zoom'
            }
          ],
          'RelatedControls': [
            'GridView',
            'ListView'
          ]
        }
      ]
    },
    {
      'UniqueId': 'Layout',
      'Title': 'Layout',
      'Subtitle': '',
      'ImagePath': '',
      'Description': 'Use layout controls to arrange the controls and content in your app.',
      'Items': [
        {
          'UniqueId': 'Border',
          'Title': 'Border',
          'Subtitle': 'A container control that draws a boundary line, background, or both, around another object.',
          'ImagePath': 'ms-appx:///Assets/Border.png',
          'Description': 'Use a Border control to draw a boundary line, background, or both, around another object. A Border can contain only one child object.',
          'Content': '<p>A <b>Border</b> can contain only one child object. If you want to put a border around multiple objects, first wrap them in a container object such as <b>StackPanel</b> and make the <b>StackPanel</b> the child of the <b>Border</b>.</p><p>You can change the appearance of a <b>Border</b> by setting basic properties:</p><ul><li>Width/Height</li><li>BorderBrush</li><li>BorderThickness</li><li>Background</li></ul><p></p><p>Most of the control pages in this app have XAML blocks to show basic usage. The appearance of the XAML blocks is defined by a Border control. Look at the <i>CodeBorderStyle</i> resource in App.xaml to see the <b>Style</b> that's applied to the Borders.</p><p>Look at the <i>BorderPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Border - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.border'
            }
          ],
          'RelatedControls': [
            'Canvas',
            'Grid',
            'StackPanel',
            'VariableSizedWrapGrid',
            'RelativePanel'
          ]
        },
        {
          'UniqueId': 'Canvas',
          'Title': 'Canvas',
          'Subtitle': 'A layout panel that supports absolute positioning of child elements relative to the top left corner of the canvas.',
          'ImagePath': 'ms-appx:///Assets/Canvas.png',
          'Description': 'The Canvas provides absolute positioning of controls or content. Content is positioned relative to the Canvas using the Canvas.Top and Canvas.Left attached properties.',
          'Content': '<p>Items are positioned on the <b>Canvas</b> using the <b>Canvas.Top</b> and <b>Canvas.Left</b> attached properties. Use the sliders to change these properties for the red rectangle and see how it moves around.</p><p>To see the effect of the <b>ZIndex</b> attached property, make sure the red rectangle is overlapping the other rectangles.</p><p>Look at the <i>CanvasPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Canvas - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.canvas'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/layout/layout-panels'
            }
          ],
          'RelatedControls': [
            'Border',
            'Grid',
            'StackPanel',
            'VariableSizedWrapGrid',
            'RelativePanel'
          ]
        },
        {
          'UniqueId': 'ItemsRepeater',
          'Title': 'ItemsRepeater',
          'Subtitle': 'A flexible, primitive control for data-driven layouts.',
          'ImagePath': 'ms-appx:///Assets/ListView.png',
          'Description': 'The ItemsRepeater is like a markup-based loop that supports virtualization.',
          'Content': '<p>A <b>ItemsRepeater</b> is more basic than an <b>ItemsControl</b>.  It does not have default styling or a control template.  It can contain a collection of items of any type. To populate the view, set the <b>ItemsSource</b> property to a data source.</p><p>Set an <b>ItemTemplate</b> to define the look of individual items.</p><p>And, optionally set the <b>Layout</b> to define how items are sized and positioned. By default, it uses a simple vertical stacking layout.</p><p>Look at the <i>ItemsRepeaterPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'ItemsRepeater - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.itemsrepeater?view=winui-2.2'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/items-repeater'
            },
            {
              'Title': 'StackLayout - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.stacklayout?view=winui-2.3'
            },
            {
              'Title': 'UniformGridLayout - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.uniformgridlayout?view=winui-2.3'
            }
          ],
          'RelatedControls': [
            'ScrollViewer',
            'ListView',
            'GridView'
          ]
        },
        {
          'UniqueId': 'Grid',
          'Title': 'Grid',
          'Subtitle': 'A layout panel that supports arranging child elements in rows and columns. ',
          'ImagePath': 'ms-appx:///Assets/Grid.png',
          'Description': 'The Grid is used to arrange controls and content in rows and columns. Content is positioned in the grid using Grid.Row and Grid.Column attached properties.',
          'Content': '<p>Look at the <i>GridPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Grid - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.grid'
            },
            {
              'Title': 'Tutorial',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/layout/grid-tutorial'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/layout/layout-panels#grid'
            }
          ],
          'RelatedControls': [
            'Border',
            'Canvas',
            'StackPanel',
            'VariableSizedWrapGrid',
            'RelativePanel'
          ]
        },
                {
          'UniqueId': 'RadioButtons',
          'Title': 'RadioButtons',
          'Subtitle': 'A control that displays a group of mutually exclusive options with keyboarding and accessibility support.',
          'ImagePath': 'ms-appx:///Assets/RadioButtons.png',
          'Description': 'A control that displays a group of mutually exclusive options with keyboarding and accessibility support.',
          'Content': '<p>Look at the <i>RadioButtonsPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'RadioButtons - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.radiobuttons?view=winui-2.4'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/radio-button'
            }
          ],
          'RelatedControls': [
            'CheckBox',
            'RadioButton',
            'ToggleSwitch',
            'ToggleButton'
          ]
        },
        {
          'UniqueId': 'RelativePanel',
          'Title': 'RelativePanel',
          'Subtitle': 'A panel that uses relationships between elements to define layout.',
          'ImagePath': 'ms-appx:///Assets/RelativePanel.png',
          'Description': 'Use a RelativePanel to layout elements by defining the relationships between elements and in relation to the panel.',
          'Content': '<p>Look at the <i>RelativePanelPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'RelativePanel - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.relativepanel'
            },
            {
              'Title': 'Guidance',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/layout/layout-panels'
            }
          ],
          'RelatedControls': [
            'Grid',
            'StackPanel',
            'Border',
            'Canvas',
            'ViewBox'
          ]
        },
        {
          'UniqueId': 'SplitView',
          'Title': 'SplitView',
          'Subtitle': 'A container that has 2 content areas, with multiple display options for the pane.',
          'ImagePath': 'ms-appx:///Assets/SplitView.png',
          'Description': 'Use a SplitView to display content, such as navigation options, in a pane on the side.  There are multiple options for displaying the pane, namely CompactOverlay, Compact, Overlay, Inline.  If you are looking for a reference to the hamburger pattern, please see the links in the documentation below.',
          'Content': '<p>Look at the <i>SplitViewPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'SplitView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.splitview'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/split-view'
            }
          ],
          'RelatedControls': [
            'StackPanel',
            'ListView',
            'GridView',
            'Grid',
            'RelativePanel'
          ]
        },
        {
          'UniqueId': 'StackPanel',
          'Title': 'StackPanel',
          'Subtitle': 'A layout panel that arranges child elements into a single line that can be oriented horizontally or vertically.',
          'ImagePath': 'ms-appx:///Assets/StackPanel.png',
          'Description': 'A StackPanel is used to arrange items in a line, either horizontally or vertically.',
          'Content': '<p>Look at the <i>StackPanelPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'StackPanel - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.stackpanel'
            },
            {
              'Title': 'Guidance',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/layout/layout-panels'
            }
          ],
          'RelatedControls': [
            'Border',
            'Canvas',
            'Grid',
            'VariableSizedWrapGrid',
            'RelativePanel'
          ]
        },
        {
          'UniqueId': 'VariableSizedWrapGrid',
          'Title': 'VariableSizedWrapGrid',
          'Subtitle': 'A layout panel that supports arranging child elements in rows and columns. Each child element can span multiple rows and columns.',
          'ImagePath': 'ms-appx:///Assets/VariableSizedWrapGrid.png',
          'Description': 'A VariableSizedWrapGrip is used to create grid layouts where content can span multiple rows and columns.',
          'Content': '<p>Elements are arranged in rows or columns that automatically wrap to a new row or column when the <b>MaximumRowsOrColumns</b> value is reached.</p><p>Whether elements are arranged in rows or columns is specified by the <b>Orientation</b> property.</p><p>Elements can span multiple rows and columns using <b>VariableSizedWrapGrid.RowSpan</b> and <b>VariableSizedWrapGrid.ColumnSpan</b> attached properties.</p><p>Elements are sized as specified by the <b>ItemHeight</b> and <b>ItemWidth</b> properties.</p><p>Look at the <i>VariableSizedWrapGridPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'VariableSizedWrapGrid - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.variablesizedwrapgrid'
            },
            {
              'Title': 'Guidance',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/layout/layout-panels'
            }
          ],
          'RelatedControls': [
            'Border',
            'Canvas',
            'Grid',
            'StackPanel',
            'RelativePanel'
          ]
        },
        {
          'UniqueId': 'ViewBox',
          'Title': 'ViewBox',
          'Subtitle': 'A container control that scales its content to a specified size.',
          'ImagePath': 'ms-appx:///Assets/ViewBox.png',
          'Description': 'Use a ViewBox control scale content up or down to a specified size.',
          'Content': '<p>Look at the <i>ViewBoxPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Viewbox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.viewbox'
            }
          ],
          'RelatedControls': [
            'ScrollViewer',
            'Canvas',
            'Grid',
            'StackPanel',
            'RelativePanel'
          ]
        }
      ]
    },
    {
      'UniqueId': 'Navigation',
      'Title': 'Navigation',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'NavigationView',
          'Title': 'NavigationView',
          'Subtitle': 'Common vertical layout for top-level areas of your app via a collapsible navigation menu.',
          'ImagePath': 'ms-appx:///Assets/NavigationView.png',
          'Description': 'The navigation view control provides a common vertical layout for top-level areas of your app via a collapsible navigation menu.',
          'Content': '',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'NavigationView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.NavigationView'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/navigationview'
            }
          ],
          'RelatedControls': [
            'SplitView',
            'Pivot'
          ]
        },
        {
          'UniqueId': 'Pivot',
          'Title': 'Pivot',
          'Subtitle': 'Presents information from different sources in a tabbed view.',
          'ImagePath': 'ms-appx:///Assets/Pivot.png',
          'Description': 'A Pivot allows you to show a collection of items from different sources in a tabbed view.',
          'Content': '<p>This page shows a simplified <b>Pivot</b> control with minimal content to demonstrate basic <b>Pivot</b> usage. Look at the <i>PivotPage.xaml</i> file in Visual Studio to see the full code for this page.</p><p>A <b>Pivot</b> control typically takes up the full page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Pivot - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.pivot'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/pivot'
            }
          ],
          'RelatedControls': [
            'SemanticZoom',
            'ListView',
            'GridView'
          ]
        },
        {
          'UniqueId': 'TabView',
          'Title': 'TabView',
          'Subtitle': 'A control that displays a collection of tabs that can be used to display several documents.',
          'ImagePath': 'ms-appx:///Assets/DefaultIcon.png',
          'Description': 'TabView provides the user with a collection of tabs that can be used to display several documents.',
          'Content': '',
          'IsNew': true,
          'IsUpdated': false,
          'IsPreview': false,
          'Docs': [
            {
              'Title': 'TabView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.tabview?view=winui-2.2'
            },
            {
              'Title': 'Guidance',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/tab-view'
            },
            {
              'Title': 'Show multiple views for an app',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/layout/show-multiple-views'
            }
          ],
          'RelatedControls': [
            'Pivot',
            'NavigationView',
            'ListView'
          ]
        }
      ]
    },
    {
      'UniqueId': 'Media',
      'Title': 'Media',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'AnimatedVisualPlayer',
          'Title': 'AnimatedVisualPlayer',
          'Subtitle': 'An element to render and control playback of motion graphics.',
          'ImagePath': 'ms-appx:///Assets/Animations.png',
          'Description': 'An element to render and control playback of motion graphics.',
          'Content': '<p>Look at the <i>AnimatedVisualPlayerPage.xaml</i> and <i>AnimatedVisualPlayerPage.xaml.cs</i> files in Visual Studio to see the full code for this page.</p>',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'AnimatedVisualPlayer - API',
              'Uri': 'https://www.docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer'
            },
            {
              'Title': 'Full Samples',
              'Uri': 'ms-windows-store://pdp/?productid=9N3J5TG8FF7F'
            },
            {
              'Title': 'Tutorials',
              'Uri': 'https://docs.microsoft.com/windows/communitytoolkit/animations/lottie#tutorials'
            },
            {
              'Title': 'Lottie Overview',
              'Uri': 'https://docs.microsoft.com/windows/communitytoolkit/animations/lottie'
            },
            {
              'Title': 'Lottie Windows - GitHub',
              'Uri': 'https://www.docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer'
            }
          ]
        },
        {
          'UniqueId': 'Image',
          'Title': 'Image',
          'Subtitle': 'A control to display image content.',
          'ImagePath': 'ms-appx:///Assets/Image.png',
          'Description': 'You can use an Image control to show and scale images.',
          'Content': '<p>Look at the <i>ImagePage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Image - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.image'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/images-imagebrushes'
            }
          ],
          'RelatedControls': [
            'MediaPlayerElement',
            'PersonPicture'
          ]
        },
        {
          'UniqueId': 'InkCanvas',
          'Title': 'InkCanvas',
          'Subtitle': 'A control to capture and display strokes.',
          'ImagePath': 'ms-appx:///Assets/InkCanvas.png',
          'Description': 'You can use an InkCanvas to capture strokes and display them.  You can also change the way the strokes are displayed through the InkDrawingAttributes.',
          'Content': '<p>Look at the <i>InkCanvasPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'InkCanvas - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.inkcanvas'
            },
            {
              'Title': 'InkToolbar - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.inktoolbar'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/inking-controls'
            }
          ],
          'RelatedControls': [
            'Canvas',
            'InkToolbar'
          ]
        },
        {
          'UniqueId': 'InkToolbar',
          'Title': 'InkToolbar',
          'Subtitle': 'A control to change settings of an InkCanvas.',
          'ImagePath': 'ms-appx:///Assets/InkCanvas.png',
          'Description': 'The InkToolbar is a control containing a customizable and extensible collection of buttons that activate ink-related features in an associated InkCanvas',
          'Content': '<p>Look at the <i>InkCanvasPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'InkToolbar - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.inktoolbar'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/inking-controls'
            }
          ],
          'RelatedControls': [
            'InkCanvas'
          ]
        },
        {
          'UniqueId': 'MediaPlayerElement',
          'Title': 'MediaPlayerElement',
          'Subtitle': 'A control to display video and image content.',
          'ImagePath': 'ms-appx:///Assets/MediaElement.png',
          'Description': 'You can use a MediaPlayerElement control to playback videos and show images. You can show transport controls or make the video autoplay.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'MediaPlayerElement - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.MediaPlayerElement'
            },
            {
              'Title': 'Media Playback',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/media-playback'
            }
          ],
          'RelatedControls': [
            'Image',
            'AnimatedVisualPlayer'
          ]
        },
        {
          'UniqueId': 'PersonPicture',
          'Title': 'PersonPicture',
          'Subtitle': 'Displays the picture of a person/contact.',
          'ImagePath': 'ms-appx:///Assets/PersonPicture.png',
          'Description': 'Displays the picture of a person/contact.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'PersonPicture - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.PersonPicture'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/person-picture'
            }
          ],
          'RelatedControls': [
            'Image'
          ]
        },
        {
          'UniqueId': 'Sound',
          'Title': 'Sound',
          'Subtitle': 'A C#-only API that enables 2D and 3D UI sounds on all UWP controls.',
          'ImagePath': 'ms-appx:///Assets/Sound.png',
          'Description': 'Sound is enabled by default for UWP apps running on Xbox, but can be set to always play on all devices if desired. Sound may also be put into Spatial Audio mode for a more immersive 10ft experience.',
          'Content': '<p>Look at the <i>SoundPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Sound - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.elementsoundplayer'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/style/sound'
            }
          ]
        },
        {
          'UniqueId': 'WebView',
          'Title': 'WebView',
          'Subtitle': 'A control that hosts HTML content in an app.',
          'ImagePath': 'ms-appx:///Assets/WebView.png',
          'Description': 'A control that hosts HTML content in an app.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'WebView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.WebView'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/web-view'
            },
            {
              'Title': 'Full Sample',
              'Uri': 'https://go.microsoft.com/fwlink/p/?LinkId=722922'
            }
          ],
          'RelatedControls': [
            'MediaPlayerElement'
          ]
        }
      ]
    },
    {
      'UniqueId': 'Styles',
      'Title': 'Styles',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'Acrylic',
          'Title': 'Acrylic',
          'Subtitle': 'A translucent material recommended for panel backgrounds.',
          'ImagePath': 'ms-appx:///Assets/AcrylicBrush.png',
          'Description': 'A translucent material recommended for panel backgrounds.',
          'Content': '',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'Acrylic - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.media.acrylicbrush'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/style/acrylic'
            }
          ],
          'RelatedControls': [
            'Reveal'
          ]
        },
        {
          'UniqueId': 'RadialGradientBrush',
          'Title': 'RadialGradientBrush',
          'Subtitle': 'A brush to show radial gradients',
          'ImagePath': 'ms-appx:///Assets/Canvas.png',
          'Description': 'Paints an area with a radial gradient. A center point defines the beginning of the gradient, and a radius defines the end point of the gradient.',
          'Content': 'The RadialGradientBrush is similar in programming model to the LinearGradientBrush. However, the linear gradient has a start and an end point to define the gradient vector, while the radial gradient has a circle, along with a focal point, to define the gradient behavior. The circle defines the end point of the gradient. The parameters can be provided as a ratio or absolute value by picking the mapping mode.',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'RadialGradientBrush - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.media.RadialGradientBrush'
            }
          ],
          'RelatedControls': [
            'Acrylic',
            'ColorPaletteResources'
          ]
        },
        {
          'UniqueId': 'Reveal',
          'Title': 'Reveal',
          'Subtitle': 'A material that changes color near the mouse.',
          'ImagePath': 'ms-appx:///Assets/Reveal.png',
          'Description': 'A material that changes color near the mouse.',
          'Content': '',
          'Docs': [
            {
              'Title': 'RevealBrush - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.media.revealbrush'
            },
            {
              'Title': 'RevealBackgroundBrush - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.media.revealbackgroundbrush'
            },
            {
              'Title': 'RevealBorderBrush - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.media.revealborderbrush'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/style/reveal'
            }
          ],
          'RelatedControls': [
            'Acrylic'
          ]
        },
        {
          'UniqueId': 'RevealFocus',
          'Title': 'Reveal Focus',
          'Subtitle': 'Customize the style of your focus rectangles.',
          'ImagePath': 'ms-appx:///Assets/RevealFocus.png',
          'Description': 'Reveal focus allows you to adapt focus rectangles to look like the focus rectangles available on Xbox.',
          'Content': '<p>Look at the <i>RevealFocusPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Reveal Focus - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.application.focusvisualkind#Windows_UI_Xaml_Application_FocusVisualKind'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/style/reveal-focus'
            }
          ],
          'RelatedControls': [
            'Reveal'
          ]
        },
        {
          'UniqueId': 'ColorPaletteResources',
          'Title': 'ColorPaletteResources',
          'Subtitle': 'A type of resource that allows you to apply custom colors to your controls.',
          'ImagePath': 'ms-appx:///Assets/DefaultIcon.png',
          'Description': 'Apply custom colors to your controls through this cascading API, or scope them to a local subset.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Theme Editor (GitHub)',
              'Uri': 'https://github.com/Microsoft/fluent-xaml-theme-editor'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/style/color#scoping-with-a-resourcedictionary'
            },
            {
              'Title': 'ColorPaletteResources - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.colorpaletteresources'
            }
          ]
        },
        {
          'UniqueId': 'CompactSizing',
          'Title': 'Compact Sizing',
          'Subtitle': 'How to use a Resource Dictionary to enable compact sizing.',
          'ImagePath': 'ms-appx:///Assets/CompactSizing.png',
          'Description': 'Compact dictionary included in WinUI 2.1. Allows you to create compact smaller apps by adding this at the page or the grid level.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Spacing',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/style/spacing'
            }
          ]
        }
      ]
    },
    {
      'UniqueId': 'Text',
      'Title': 'Text',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'AutoSuggestBox',
          'Title': 'AutoSuggestBox',
          'Subtitle': 'A control to provide suggestions as a user is typing.',
          'ImagePath': 'ms-appx:///Assets/AutoSuggestBox.png',
          'Description': 'A text control that makes suggestions to users as they type. The app is notified when text has been changed by the user and is responsible for providing relevant suggestions for this control to display.',
          'Content': '<p>Look at the <i>AutoSuggestBoxPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'AutoSuggestBox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.autosuggestbox'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/auto-suggest-box'
            }
          ],
          'RelatedControls': [
            'ListBox',
            'ComboBox',
            'TextBox'
          ]
        },
        {
          'UniqueId': 'NumberBox',
          'Title': 'NumberBox',
          'Subtitle': 'A text control used for numeric input and evaluation of algebraic equations.',
          'ImagePath': 'ms-appx:///Assets/NumberBox.png',
          'Description': 'Use NumberBox to allow users to enter algebraic equations and numeric input in your app.',
          'Content': '<p>Look at the <i>NumberBox.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': true,
          'Docs': [
            {
              'Title': 'NumberBox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/microsoft.ui.xaml.controls.numberbox'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/number-box'
            }
          ],
          'RelatedControls': [
            'TextBox',
            'TextBlock',
            'RichTextBlock',
            'RichEditBox'
          ]
        },
        {
          'UniqueId': 'PasswordBox',
          'Title': 'PasswordBox',
          'Subtitle': 'A control for entering passwords.',
          'ImagePath': 'ms-appx:///Assets/PasswordBox.png',
          'Description': 'A user can enter a single line of non-wrapping text in a PasswordBox control. The text is masked by characters that you can specify by using the PasswordChar property, and you can specify the maximum number of characters that the user can enter by setting the MaxLength property.',
          'Content': '<p>Look at the <i>PasswordBoxPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'PasswordBox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.passwordbox'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/text-controls'
            }
          ],
          'RelatedControls': [
            'TextBox',
            'TextBlock',
            'RichTextBlock',
            'RichEditBox'
          ]
        },
        {
          'UniqueId': 'RichEditBox',
          'Title': 'RichEditBox',
          'Subtitle': 'A rich text editing control that supports formatted text, hyperlinks, and other rich content.',
          'ImagePath': 'ms-appx:///Assets/RichEditBox.png',
          'Description': 'You can use a RichEditBox control to enter and edit rich text documents that contain formatted text, hyperlinks, and images. By default, the RichEditBox supports spell checking. You can make a RichEditBox read-only by setting its IsReadOnly property to true.',
          'Content': '<p>On this page, you can type into the <b>RichTextBox</b> control and save it as a RichTextFormat (.rtf) document, or load an existing .rtf document. You can format the text as <b>Bold</b> or <u>Underlined</u>, and change the text color.</p><p>Look at the <i>RichEditBoxPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsUpdated': true,
          'Docs': [
            {
              'Title': 'RichEditBox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.richeditbox'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/text-controls'
            }
          ],
          'RelatedControls': [
            'NumberBox',
            'TextBox',
            'RichTextBlock',
            'TextBlock'
          ]
        },
        {
          'UniqueId': 'RichTextBlock',
          'Title': 'RichTextBlock',
          'Subtitle': 'A control that displays formatted text, hyperlinks, inline images, and other rich content.',
          'ImagePath': 'ms-appx:///Assets/RichTextBlock.png',
          'Description': 'RichTextBlock provides more advanced formatting features than the TextBlock control. You can apply character and paragraph formatting to the text in the RichTextBlock. For example, you can apply Bold, Italic, and Underline to any portion of the text in the control. You can use linked text containers (a RichTextBlock linked to RichTextBlockOverflow elements) to create advanced page layouts.',
          'Content': '<p>Change the width of the app to see how the <b>RichTextBlock</b> overflows into additional columns as the app gets narrower.</p><p>Look at the <i>RichTextBlockPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'RichTextBlock - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.richtextblock'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/text-controls'
            }
          ],
          'RelatedControls': [
            'NumberBox',
            'TextBlock',
            'TextBox',
            'PasswordBox',
            'RichEditBox',
            'WebView'
          ]
        },
        {
          'UniqueId': 'TextBlock',
          'Title': 'TextBlock',
          'Subtitle': 'A lightweight control for displaying small amounts of text.',
          'ImagePath': 'ms-appx:///Assets/TextBlock.png',
          'Description': 'TextBlock is the primary control for displaying read-only text in your app. You typically display text by setting the Text property to a simple string. You can also display a series of strings in Run elements and give each different formatting.',
          'Content': '<p>Look at the <i>TextBlockPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'TextBlock - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.textblock'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/text-controls'
            }
          ],
          'RelatedControls': [
            'NumberBox',
            'TextBox',
            'RichTextBlock',
            'PasswordBox',
            'RichEditBox'
          ]
        },
        {
          'UniqueId': 'TextBox',
          'Title': 'TextBox',
          'Subtitle': 'A single-line or multi-line plain text field.',
          'ImagePath': 'ms-appx:///Assets/TextBox.png',
          'Description': 'Use a TextBox to let a user enter simple text input in your app. You can add a header and placeholder text to let the user know what the TextBox is for, and you can customize it in other ways.',
          'Content': '<p>Look at the <i>TextBoxPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'TextBox - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.textbox'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/controls-and-patterns/text-controls'
            }
          ],
          'RelatedControls': [
            'NumberBox',
            'TextBlock',
            'RichTextBlock',
            'PasswordBox',
            'RichEditBox',
            'AutoSuggestBox'
          ]
        }
      ]
    },
    {
      'UniqueId': 'Motion',
      'Title': 'Motion',
      'Subtitle': '',
      'ImagePath': '',
      'Description': '',
      'Items': [
        {
          'UniqueId': 'ConnectedAnimation',
          'Title': 'Connected Animation',
          'Subtitle': 'Connected animations continue elements during page navigation and help the user maintain their context between views.',
          'ImagePath': 'ms-appx:///Assets/ConnectedAnimations.png',
          'Description': 'Connected animations continue elements during page navigation and help the user maintain their context between views.',
          'Content': '<p>Look at the <i>ConnectedAnimationPage.xaml</i> and <i>ConnectedAnimationPage.xaml.cs</i> files in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'ConnectedAnimation - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.media.animation.connectedanimation'
            },
            {
              'Title': 'ConnectedAnimationService - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.media.animation.connectedanimationservice'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/connected-animation'
            },
            {
              'Title': 'Quickstart: Motion',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/'
            }
          ],
          'RelatedControls': [
            'PageTransition',
            'ThemeTransition'
          ]
        },
        {
          'UniqueId': 'EasingFunction',
          'Title': 'Easing Functions',
          'Subtitle': 'Easing is a way to manipulate the velocity of an object as it animates.',
          'ImagePath': 'ms-appx:///Assets/EasingFunctions.png',
          'Description': 'Easing is a way to manipulate the velocity of an object as it animates.',
          'Content': '<p>Look at the <i>EasingFunctionPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'EasingFunctionBase - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.media.animation.easingfunctionbase'
            },
            {
              'Title': 'Timing and Easing',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/timing-and-easing'
            },
            {
              'Title': 'Quickstart: Motion',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/'
            }
          ],
          'RelatedControls': [
            'ConnectedAnimation',
            'PageTransition',
            'ThemeTransition'
          ]
        },
        {
          'UniqueId': 'PageTransition',
          'Title': 'Page Transitions',
          'Subtitle': 'Page transitions provide visual feedback about the relationship between pages.',
          'ImagePath': 'ms-appx:///Assets/Transitions.png',
          'Description': 'Page transitions provide visual feedback about the relationship between pages.',
          'Content': '<p>Look at the <i>PageTransitionPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'NavigationThemeTransition - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Media.Animation.NavigationThemeTransition'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/page-transitions'
            },
            {
              'Title': 'Quickstart: Motion',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/'
            }
          ],
          'RelatedControls': [
            'ConnectedAnimation',
            'ThemeTransition'
          ]
        },
        {
          'UniqueId': 'ThemeTransition',
          'Title': 'Theme Transitions',
          'Subtitle': 'Theme transitions are pre-packaged, easy-to-apply animations.',
          'ImagePath': 'ms-appx:///Assets/Transitions.png',
          'Description': 'Theme transitions are pre-packaged, easy-to-apply animations.',
          'Content': '<p>Look at the <i>ThemeTransitionPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'Transitions - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.transitions#Windows_UI_Xaml_UIElement_Transitions'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/xaml-animation#animations-available-in-the-library'
            },
            {
              'Title': 'Quickstart: Motion',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/'
            }
          ],
          'RelatedControls': [
            'ImplicitTransition',
            'PageTransition'
          ]
        },
        {
          'UniqueId': 'XamlCompInterop',
          'Title': 'Animation interop',
          'Subtitle': 'XAML and Composition interop allows you to animate elements using expressions, natural animations, and more.',
          'ImagePath': 'ms-appx:///Assets/StoryboardAnimation.png',
          'IsNew': false,
          'Description': 'XAML and Composition interop allows you to animate elements using expressions, natural animations, and more',
          'Content': '<p>Look at the <i>XamlCompInterop.xaml.cs</i> file in Visual Studio to see the full code for this page.</p>',
          'Docs': [
            {
              'Title': 'Quickstart: Motion',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/'
            },
            {
              'Title': 'Composition Animation - API',
              'Uri': 'https://docs.microsoft.com/windows/uwp/composition/composition-animation'
            },
            {
              'Title': 'Guidelines - Xaml Property Animations',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/xaml-property-animations'
            },
            {
              'Title': 'Using the Visual Layer with XAML',
              'Uri': 'https://docs.microsoft.com/windows/uwp/composition/using-the-visual-layer-with-xaml'
            }
          ],
          'RelatedControls': [
            'EasingFunction'
          ]
        },
        {
          'UniqueId': 'ImplicitTransition',
          'Title': 'Implicit Transitions',
          'Subtitle': 'Use Implicit Transitions to automatically animate changes to properties.',
          'ImagePath': 'ms-appx:///Assets/Transitions.png',
          'IsNew': false,
          'Description': 'Use Implicit Transitions to automatically animate changes to properties.',
          'Content': '<p>Look at the <i>ImplicitTransitionPage.xaml</i> file in Visual Studio to see the full code for this page.</p>',
          'Docs': [
            {
              'Title': 'Transitions - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.transitions#Windows_UI_Xaml_UIElement_Transitions'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/motion-in-practice#implicit-animations'
            },
            {
              'Title': 'Quickstart: Motion',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/'
            }
          ],
          'RelatedControls': [
            'PageTransition',
            'ThemeTransition'
          ]
        },
        {
          'UniqueId': 'ParallaxView',
          'Title': 'ParallaxView',
          'Subtitle': 'A container control that provides the parallax effect when scrolling.',
          'ImagePath': 'ms-appx:///Assets/ParallaxView.png',
          'Description': 'A container control that provides the parallax effect when scrolling.',
          'Content': '',
          'IsNew': false,
          'Docs': [
            {
              'Title': 'ParallaxView - API',
              'Uri': 'https://docs.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.Parallaxview'
            },
            {
              'Title': 'Guidelines',
              'Uri': 'https://docs.microsoft.com/windows/uwp/design/motion/parallax'
            }
          ],
          'RelatedControls': [
            'ScrollViewer'
          ]
        }
      ]
    }
  ]
}
";


    }
}