using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace NotepadUWP
{
    public sealed partial class SettingPage : UserControl
    {
        public MainPage mainPage;
        public SettingPage()
        {
            this.InitializeComponent();
        }

        //Page Functions
        public void SetPageControls()       //初始化设定，Initializing the Setting Page
        {
            //Set StatusBarSwicher and LineWrapSetter Values
            this.StatusBarSwitcher.IsOn = (bool)mainPage.localSettings.Values["StatusBarVisibility"];
            this.LineWrapSetter.IsOn = (bool)mainPage.localSettings.Values["IsWrap"];

            //Initializing Font Part
            //Get installed fonts list
            string[] installedFonts = Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies();
            foreach (string font in installedFonts)
            {
                this.FontFamilySelectingComboBox.Items.Add(new ComboBoxItem
                {
                    Name = font,
                    Content = new TextBlock
                    {
                        Text = font,
                        FontFamily = new FontFamily(font),
                    }
                });
            }
            //Set selected Fontfamily item
            foreach (ComboBoxItem c in this.FontFamilySelectingComboBox.Items)
            {
                if(c.Name == (string)mainPage.localSettings.Values["FontFamily"])
                {
                    this.FontFamilySelectingComboBox.SelectedItem = c;
                }
            }
            //Set selected FontSize item
            this.FontSizeSelectingComboBox.SelectedItem = double.Parse(mainPage.localSettings.Values["FontSize"].ToString());;

            //Add ComboBox Items to Search Engine Selecting ComboBox
            XmlDocument searchEngineXml = new XmlDocument();
            searchEngineXml.Load("Resources/SearchEnginesURL.xml");
            XmlNode xmlNode = searchEngineXml.SelectSingleNode("SearchEngines");
            XmlNodeList xmlNodeList = xmlNode.ChildNodes;
            //May cause exception if SearchEnginesURL.xml's format is bad
            try
            {
                //Read Search Engines from Xml
                foreach (XmlNode node in xmlNodeList)
                {
                    this.SearchEngineComboBox.Items.Add(new ComboBoxItem
                    {
                        Name = node.Attributes["Name"].Value,
                        Content = new TextBlock { Text = node.Attributes["SearchEngineName"].Value },
                    });
                }
                //Set Selected Item of Search Engine ComboBox
                foreach (ComboBoxItem c in this.SearchEngineComboBox.Items)
                {
                    if (c.Name == (string)mainPage.localSettings.Values["SearchEngine"])
                    {
                        this.SearchEngineComboBox.SelectedItem = c;
                    }
                }
            }
            catch
            {
                this.SearchEngineComboBox.Items.Clear();
                //add a warning to search engine selecting combobox
                this.SearchEngineComboBox.Items.Add(new ComboBoxItem { Content = "无法读取搜索引擎" });
                
            }
        }


        //Setting Page Events
        private void LineWrapSetterToggled(object sender, RoutedEventArgs e)    //换行按钮切换
        {
            mainPage.localSettings.Values["IsWrap"] = this.LineWrapSetter.IsOn;
            mainPage.ApplySettings(this,null);
        }
        private void StatusBarSwitcherToggled(object sender, RoutedEventArgs e)     //状态栏可见性按钮切换
        {
            mainPage.localSettings.Values["StatusBarVisibility"] = this.StatusBarSwitcher.IsOn;
            mainPage.ApplySettings(this, null);
        }
        private void SearchEngineComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)   //搜索引擎改变
        {
            mainPage.localSettings.Values["SearchEngine"] = ((ComboBoxItem)(this.SearchEngineComboBox.SelectedItem)).Name;
        }
        private void FontFamilySelectingComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)    //FontFamily Selection Changed
        {
            mainPage.localSettings.Values["FontFamily"] = ((ComboBoxItem)(this.FontFamilySelectingComboBox.SelectedItem)).Name;
            mainPage.ApplySettings(this, null);
        }

        private void FontSizeSelectingComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mainPage.localSettings.Values["FontSize"] = this.FontSizeSelectingComboBox.SelectedItem;
            mainPage.ApplySettings(this, null);
        }

        private async void FontSizeSelectingComboBoxTextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            double inputFontSize = 0;
            if (double.TryParse(this.FontSizeSelectingComboBox.Text, out inputFontSize))
            {
                //if input too small or too big
                if(inputFontSize < 5 || inputFontSize > 300)
                {
                    ContentDialog invalidDialog = new ContentDialog
                    {
                        Content = "字号只能为 5 到 300 之间的数字",
                        CloseButtonText = "确定",
                    };
                    await invalidDialog.ShowAsync();
                    return;
                }

                mainPage.localSettings.Values["FontSize"] = inputFontSize;
                mainPage.ApplySettings(this, null);
                return;
            }

            //if input illegal
            else
            {
                ContentDialog invalidDialog = new ContentDialog
                {
                    Content = "字号只能为 5 到 300 之间的数字",
                    CloseButtonText = "确定",
                };
                await invalidDialog.ShowAsync();
                return;
            }
        }
    }
}
