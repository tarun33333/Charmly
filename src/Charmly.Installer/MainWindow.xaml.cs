using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Charmly.Installer;

public partial class MainWindow : Window
{
    private int currentStep = 0;
    private readonly string defaultInstallPath;

    public MainWindow()
    {
        Program.Log("MainWindow ctor: InitializeComponent starting...");
        InitializeComponent();
        Program.Log("MainWindow ctor: InitializeComponent finished.");

        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.RelativeOrAbsolute);
            var bmp = new System.Windows.Media.Imaging.BitmapImage(iconUri);
            Icon = bmp;
            HeaderIcon.Source = bmp;
        }
        catch (Exception ex)
        {
            Program.Log($"MainWindow ctor: Non-fatal icon load exception: {ex.Message}");
        }

        defaultInstallPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Charmly");

        InstallPathBox.Text = defaultInstallPath;
        LoadDisclaimerText();
        UpdateStepView();
        Program.Log("MainWindow ctor: Completed successfully.");
    }

    private void LoadDisclaimerText()
    {
        DisclaimerTextBox.Text =
@"MIT License - Copyright (c) 2026 Tarun

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

======================================================================
LEGAL & TRADEMARK DISCLAIMER:

Charmly is an independent, non-commercial open-source desktop personalization
utility developed and maintained by Tarun under the MIT License.

All charms, amulets, and symbols included in this software are original,
generic, or parody artistic interpretations. Any references to cultural,
mythological, or pop-culture motifs are strictly for non-commercial fan
appreciation and parody under fair-use principles, and do not imply any
affiliation, sponsorship, or endorsement by any trademark or copyright
holders.
======================================================================";
    }

    private void UpdateStepView()
    {
        PageWelcome.Visibility = currentStep == 0 ? Visibility.Visible : Visibility.Collapsed;
        PageDisclaimer.Visibility = currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        PageOptions.Visibility = currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        PageInstalling.Visibility = currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
        PageFinished.Visibility = currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.IsEnabled = currentStep > 0 && currentStep < 3;
        BackButton.Visibility = currentStep >= 3 ? Visibility.Collapsed : Visibility.Visible;

        if (currentStep == 0)
        {
            HeaderTitle.Text = "Welcome to Charmly Setup";
            HeaderSubtitle.Text = "Interactive Desktop Charms for Windows by Tarun";
            NextButton.Content = "Next >";
            NextButton.IsEnabled = true;
            CancelButton.Visibility = Visibility.Visible;
        }
        else if (currentStep == 1)
        {
            HeaderTitle.Text = "License Agreement & Legal Disclaimer";
            HeaderSubtitle.Text = "Please review the agreement and disclaimer before continuing";
            NextButton.Content = "Next >";
            NextButton.IsEnabled = AgreeCheckBox.IsChecked == true;
            CancelButton.Visibility = Visibility.Visible;
        }
        else if (currentStep == 2)
        {
            HeaderTitle.Text = "Installation Options";
            HeaderSubtitle.Text = "Select destination folder and startup preferences";
            NextButton.Content = "Install";
            NextButton.IsEnabled = !string.IsNullOrWhiteSpace(InstallPathBox.Text);
            CancelButton.Visibility = Visibility.Visible;
        }
        else if (currentStep == 3)
        {
            HeaderTitle.Text = "Installing Charmly...";
            HeaderSubtitle.Text = "Please wait while files are extracted to your computer";
            NextButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
        }
        else if (currentStep == 4)
        {
            HeaderTitle.Text = "Charmly Setup Complete";
            HeaderSubtitle.Text = "Charmly has been successfully installed";
            NextButton.Content = "Finish";
            NextButton.IsEnabled = true;
            CancelButton.Visibility = Visibility.Collapsed;
        }
    }

    private void OnAgreeChanged(object sender, RoutedEventArgs e)
    {
        if (currentStep == 1)
        {
            NextButton.IsEnabled = AgreeCheckBox.IsChecked == true;
        }
    }

    private void OnBrowseFolderClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Destination Folder",
            InitialDirectory = InstallPathBox.Text
        };

        if (dialog.ShowDialog() == true)
        {
            InstallPathBox.Text = Path.Combine(dialog.FolderName, "Charmly");
        }
    }

    private void OnBackClicked(object sender, RoutedEventArgs e)
    {
        if (currentStep > 0 && currentStep < 3)
        {
            currentStep--;
            UpdateStepView();
        }
    }

    private async void OnNextClicked(object sender, RoutedEventArgs e)
    {
        if (currentStep < 2)
        {
            currentStep++;
            UpdateStepView();
        }
        else if (currentStep == 2)
        {
            currentStep = 3;
            UpdateStepView();
            await PerformInstallationAsync();
        }
        else if (currentStep == 4)
        {
            if (LaunchCheckBox.IsChecked == true)
            {
                string exePath = Path.Combine(InstallPathBox.Text.Trim(), "Charmly.exe");
                if (!File.Exists(exePath))
                {
                    exePath = Path.Combine(InstallPathBox.Text.Trim(), "Hangly.exe");
                }
                if (File.Exists(exePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = InstallPathBox.Text.Trim(),
                        UseShellExecute = true
                    });
                }
            }
            Close();
        }
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        if (currentStep < 3)
        {
            var res = MessageBox.Show(
                "Are you sure you want to cancel the installation?",
                "Cancel Setup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                Close();
            }
        }
    }

    private async Task PerformInstallationAsync()
    {
        string targetDir = InstallPathBox.Text.Trim();
        bool enableStartup = StartupCheckBox.IsChecked == true;
        bool createDesktopShortcut = DesktopShortcutCheckBox.IsChecked == true;
        bool createStartMenuShortcut = StartMenuCheckBox.IsChecked == true;

        await Task.Run(() =>
        {
            try
            {
                InstallerEngine.Install(
                    targetDir,
                    enableStartup,
                    createDesktopShortcut,
                    createStartMenuShortcut,
                    (pct, status) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            InstallProgress.Value = pct;
                            InstallingStatus.Text = status;
                        });
                    });

                Dispatcher.Invoke(() =>
                {
                    InstallProgress.Value = 100;
                    currentStep = 4;
                    UpdateStepView();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"An error occurred during installation:\n\n{ex.Message}",
                        "Installation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    currentStep = 2;
                    UpdateStepView();
                });
            }
        });
    }
}
