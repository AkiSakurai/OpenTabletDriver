using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using OpenTabletDriver.Desktop;
using OpenTabletDriver.Desktop.Interop;
using OpenTabletDriver.Desktop.Reflection.Metadata;
using OpenTabletDriver.UX.Dialogs;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;

namespace OpenTabletDriver.UX.Windows.Plugins
{
    public class PluginManagerWindow : DesktopForm
    {
        public PluginManagerWindow()
            : base(Application.Instance.MainForm)
        {
            this.Title = "Plugin Manager";
            this.ClientSize = new Size(1000, 750);
            this.AllowDrop = true;

            this.Menu = ConstructMenu();
            this.Content = dropPanel = new PluginDropPanel
            {
                Content = new StackLayout
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Orientation = Orientation.Vertical,
                    Items =
                    {
                        new StackLayoutItem
                        {
                            Expand = true,
                            Control = new Splitter
                            {
                                Panel1MinimumSize = 300,
                                Panel1 = pluginList = new PluginMetadataList(),
                                Panel2 = metadataViewer = new MetadataViewer()
                            }
                        },
                        new StackLayoutItem
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Control = new Panel
                            {
                                Padding = 5,
                                Content = new Label
                                {
                                    Text = "Drag and drop plugins here to install.",
                                    VerticalAlignment = VerticalAlignment.Center
                                }
                            }
                        }
                    }
                }
            };

            metadataViewer.MetadataBinding.Bind(pluginList.SelectedItemBinding, DualBindingMode.OneWay);

            dropPanel.RequestPluginInstall += Install;
            metadataViewer.RequestPluginInstall += DownloadAndInstall;
            metadataViewer.RequestPluginUninstall += Uninstall;
        }

        private readonly PluginDropPanel dropPanel;
        private readonly PluginMetadataList pluginList;
        private readonly MetadataViewer metadataViewer;

        protected async Task SwitchRepositorySource()
        {
            var dialog = new RepositoryDialog("Switch Repository Source");
            if (await dialog.ShowModalAsync() is PluginMetadataCollection repository)
                pluginList.SetRepository(repository);
        }

        protected async Task<bool> DownloadAndInstall(PluginMetadata metadata)
        {
            try
            {
                if (await App.Driver.Instance.DownloadPlugin(metadata))
                {
                    pluginList.SelectFirstOrDefault((m => PluginMetadata.Match(m, metadata)));
                    AppInfo.PluginManager.Load();
                }
                return true;
            }
            catch (RemoteInvocationException ex)
            {
                var data = ex.DeserializedErrorData as CommonErrorData;
                if (data.TypeName == typeof(CryptographicException).FullName)
                {
                    MessageBox.Show(
                        data.Message + Environment.NewLine + "Report this incident to the developers!",
                        "Cryptographic Verification Error",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                }
                else
                {
                    data.ShowMessageBox();
                }
                return false;
            }
        }

        protected async Task Install(string path)
        {
            if (await App.Driver.Instance.InstallPlugin(path))
            {
                AppInfo.PluginManager.Load();
            }
            else
            {
                MessageBox.Show(this, $"Failed to install plugin from '{path}'", "Plugin Manager", MessageBoxType.Error);
            }
        }

        protected async Task<bool> Uninstall(PluginMetadata metadata)
        {
            var context = AppInfo.PluginManager.GetLoadedPlugins().First(
                c => PluginMetadata.Match(c.GetMetadata(), metadata)
            );

            if (context.Directory.Exists && !await App.Driver.Instance.UninstallPlugin(context.Directory.FullName))
            {
                MessageBox.Show(this, $"'{context.FriendlyName}' failed to uninstall", "Plugin Manager", MessageBoxType.Error);
                return false;
            }

            AppInfo.PluginManager.UnloadPlugin(context);
            return true;
        }

        private MenuBar ConstructMenu()
        {
            var quitCommand = new Command { MenuText = "Exit", Shortcut = Keys.Escape };
            quitCommand.Executed += (_, _) => this.Close();

            var install = new Command { MenuText = "Install plugin...", Shortcut = Application.Instance.CommonModifier | Keys.O };
            install.Executed += PromptInstallPlugin;

            var refresh = new Command { MenuText = "Refresh", Shortcut = Application.Instance.CommonModifier | Keys.R };
            refresh.Executed += RefreshHandler;

            var alternateSource = new Command { MenuText = "Use alternate source..." };
            alternateSource.Executed += async (sender, e) => await SwitchRepositorySource();

            var pluginsDirectory = new Command { MenuText = "Open plugins directory..." };
            pluginsDirectory.Executed += (sender, e) => DesktopInterop.OpenFolder(AppInfo.Current.PluginDirectory);

            return new MenuBar()
            {
                QuitItem = quitCommand,
                ApplicationItems =
                {
                    install,
                    refresh,
                    alternateSource,
                    pluginsDirectory
                }
            };
        }

        private async void PromptInstallPlugin(object sender, EventArgs e)
        {
            if (!this.ParentWindow.Enabled)
                return;

            var dialog = Extensions.OpenFileDialog(
                "Choose a plugin to install...",
                Eto.EtoEnvironment.GetFolderPath(Eto.EtoSpecialFolder.Documents),
                [new FileFilter("Plugin (.zip, .dll)", ".zip", ".dll")],
                true
            );

            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                foreach (var file in dialog.Filenames)
                {
                    await Install(file);
                }
            }
        }

        private void RefreshHandler(object sender, EventArgs e)
        {
            if (this.ParentWindow.Enabled)
                pluginList.Refresh();
        }
    }
}
