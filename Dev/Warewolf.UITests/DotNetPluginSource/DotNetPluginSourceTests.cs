﻿using System.IO;
using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Warewolf.UITests
{
    [CodedUITest]
    public class DotNetPluginSourceTests
    {
        const string DLLAssemblySourceName = "CodedUITestDLLDotNetPluginSource";
        const string GACAssemblySourceName = "CodedUITestGACDotNetPluginSource";

        [TestMethod]
        [TestCategory("Plugin Sources")]
        public void Open_DotNetPluginSource_From_ExplorerContextMenu_UITests()
        {
            UIMap.Click_NewDotNetPluginSource_From_ExplorerContextMenu();
            Assert.IsTrue(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.AssemblyComboBox.Enabled, "Assembly Combobox is not enabled");
            Assert.IsTrue(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.AssemblyDirectoryButton.Enabled, "Assembly Combobox Button is not enabled");
            Assert.IsFalse(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.ConfigFileComboBox.Enabled, "Config File Combobox is enabled");
            Assert.IsFalse(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.ConfigFileDirectoryButton.Enabled, "Config File Combobox Button is enabled");
            Assert.IsFalse(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.GACAssemblyComboBox.Enabled, "GAC Assembly Combobox is enabled");
            Assert.IsTrue(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.GACAssemblyDirectoryButton.Enabled, "GAC Assembly Combobox Button is not enabled");
            UIMap.Click_Close_DotNetPlugin_Source_Tab();
        }

        [TestMethod]
        [TestCategory("Plugin Sources")]
        public void Create_Assembly_DotNetPluginSource_UITests()
        {
            string filePath = @"C:\UITestAssembly.dll";
            var fileStream = File.Create(filePath);
            fileStream.Close();

            UIMap.Click_NewDotNetPluginSource_From_ExplorerContextMenu();
            UIMap.Click_AssemblyDirectoryButton_On_DotnetPluginSourceTab();
            Assert.IsTrue(UIMap.ChooseDLLWindow.Exists, "Choose DLL Window does not exist.");
            UIMap.Select_DLLAssemblyFile_From_ChooseDLLWindow(filePath);
            Assert.IsTrue(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.ConfigFileComboBox.Enabled, "Config File ComboBox is not enabled");
            Assert.IsTrue(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.ConfigFileDirectoryButton.Enabled, "Config File Directory button is not enabled");
            UIMap.Click_ConfigFileDirectoryButton_On_DotnetPluginSourceTab();
            UIMap.Enter_ConfigFile_In_SelectFilesWindow();
            Assert.IsFalse(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.GACAssemblyComboBox.Enabled, "GAC Assembly Combobox is enabled");
            UIMap.Save_With_Ribbon_Button_And_Dialog(DLLAssemblySourceName);
            UIMap.Filter_Explorer(DLLAssemblySourceName);
            Assert.IsTrue(UIMap.MainStudioWindow.DockManager.SplitPaneLeft.Explorer.ExplorerTree.localhost.FirstItem.Exists, "Source did not save in the explorer UI.");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            UIMap.Click_Close_DotNetPlugin_Source_Tab();
        }

        [TestMethod]
        [TestCategory("DotNetPluginSource")]
        public void Create_GACAssembly_DotNetPluginSource_UITests()
        {
            UIMap.Click_NewDotNetPluginSource_From_ExplorerContextMenu();
            Mouse.Click(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.GACAssemblyDirectoryButton);
            UIMap.Select_GACAssemblyFile_From_ChooseDLLWindow();
            UIMap.Save_With_Ribbon_Button_And_Dialog(GACAssemblySourceName);
            UIMap.Filter_Explorer(GACAssemblySourceName);
            Assert.IsTrue(UIMap.MainStudioWindow.DockManager.SplitPaneLeft.Explorer.ExplorerTree.localhost.FirstItem.Exists, "Source did not save in the explorer UI.");
            UIMap.Click_Close_DotNetPlugin_Source_Tab();
        }
        [TestMethod]
        [TestCategory("DotNetPluginSource")]
        public void SelectGACClearsDLLAssembly()
        {
            string filePath = @"C:\UITestAssembly.dll";
            var fileStream = File.Create(filePath);
            fileStream.Close();

            UIMap.Click_NewDotNetPluginSource_From_ExplorerContextMenu();
            UIMap.Click_AssemblyDirectoryButton_On_DotnetPluginSourceTab();
            Assert.IsTrue(UIMap.ChooseDLLWindow.Exists, "Choose DLL Window does not exist.");
            UIMap.Select_DLLAssemblyFile_From_ChooseDLLWindow(filePath);
            Mouse.Click(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.GACAssemblyDirectoryButton);
            UIMap.Select_GACAssemblyFile_From_ChooseDLLWindow();
            Assert.IsFalse(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.ConfigFileComboBox.Enabled, "Config File Combobox is enabled.");
            Assert.IsTrue(string.IsNullOrEmpty(UIMap.MainStudioWindow.DockManager.SplitPaneMiddle.TabManSplitPane.TabMan.DotNetPluginSourceTab.WorkSurfaceContext.AssemblyComboBox.TextEdit.Text), "Assembly Combobox did not clear text.");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            UIMap.Click_Close_DotNetPlugin_Source_Tab();
        }

        #region Additional test attributes

        [TestInitialize()]
        public void MyTestInitialize()
        {
            UIMap.SetPlaybackSettings();
            UIMap.AssertStudioIsRunning();
        }
        
        public UIMap UIMap
        {
            get
            {
                if (_UIMap == null)
                {
                    _UIMap = new UIMap();
                }

                return _UIMap;
            }
        }

        private UIMap _UIMap;

        #endregion
    }
}