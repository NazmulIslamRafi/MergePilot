using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using CheckBox = System.Windows.Controls.CheckBox;

namespace MergePilot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ValidateSelections();

        }

        private async void Merge_Click(object sender, RoutedEventArgs e)
        {
            string source = SourceBranchBox.Text.Trim();

            var repoPath = GetSelectedRepositories();
            var targetEnvs = GetSelectedEnvironments();
            var targetBranches = GetClientBranch();

            OutputBox.Text = "Merging in progress...\n";
            foreach (var repo in repoPath)
            {
                foreach (var env in targetEnvs)
                {
                    foreach (var branch in targetBranches)
                    {
                        string targetBranch = $"{branch}/deployment-{env}";
                        try
                        {
                            var result = await GitHelper.MergeBranchAsync(repo, source, targetBranch);
                            OutputBox.Text += "\n" + result + "\n";
                        }
                        catch (Exception ex)
                        {
                            ErrorBox.Text += string.IsNullOrEmpty(ErrorBox.Text.ToString()) ?
                                $"❌ Merge failed for {targetBranch}: {ex.Message}" : $"\n❌ Merge failed for {targetBranch}: {ex.Message}";
                        }
                    }
                }
            }
            OutputBox.Text += "\n✅ Merge completed!";
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ValidateSelections();
        }
        #region Private methods
        private List<string> GetClientBranch()
        {
            List<string> targetClients = new List<string>();

            // Check which client checkboxes are selected
            if (chkFirsttrip.IsChecked == true)
                targetClients.Add(chkFirsttrip.Tag.ToString());

            if (chkTriplover.IsChecked == true)
                targetClients.Add(chkTriplover.Tag.ToString());

            if (chkTravelchamp.IsChecked == true)
                targetClients.Add(chkTravelchamp.Tag.ToString());

            if (chkTakeoff.IsChecked == true)
                targetClients.Add(chkTakeoff.Tag.ToString());

            if (chkTaketrip.IsChecked == true)
                targetClients.Add(chkTaketrip.Tag.ToString());
            return targetClients;
        }
        private List<string> GetSelectedEnvironments()
        {
            List<string> selectedEnvs = new List<string>();

            if (chkDev.IsChecked == true)
                selectedEnvs.Add("dev");
            if (chkStage.IsChecked == true)
                selectedEnvs.Add("stage");
            if (chkPreProd.IsChecked == true)
                selectedEnvs.Add("pre-prod");
            if (chkProd.IsChecked == true)
                selectedEnvs.Add("prod");

            return selectedEnvs;
        }
        private List<string> GetSelectedRepositories()
        {
            List<string> selectedRepos = new List<string>();

            if (chkAuthBackend.IsChecked == true)
                selectedRepos.Add(chkAuthBackend.Tag.ToString());

            if (chkAdminBackend.IsChecked == true)
                selectedRepos.Add(chkAdminBackend.Tag.ToString());

            if (chkFlightBackend.IsChecked == true)
                selectedRepos.Add(chkFlightBackend.Tag.ToString());

            if (chkCRMBackend.IsChecked == true)
                selectedRepos.Add(chkCRMBackend.Tag.ToString());

            if (chkPushNotificationBackend.IsChecked == true)
                selectedRepos.Add(chkPushNotificationBackend.Tag.ToString());

            if (chkSchedulerBackend.IsChecked == true)
                selectedRepos.Add(chkSchedulerBackend.Tag.ToString());

            if (chkAdminFrontend.IsChecked == true)
                selectedRepos.Add(chkAdminFrontend.Tag.ToString());

            return selectedRepos;
        }
        private void ValidateSelections()
        {
            bool hasRepo = RepoStackPanel.Children.OfType<CheckBox>().Any(chk => chk.IsChecked == true);
            bool hasEnv = EnvStackPanel.Children.OfType<CheckBox>().Any(chk => chk.IsChecked == true);
            bool hasClient = ClientWrapPanel.Children.OfType<CheckBox>().Any(chk => chk.IsChecked == true);

            btnMerge.IsEnabled = hasRepo && hasEnv && hasClient;
        }

        #endregion

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void chkAllCheckUncheck_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAllCheckUncheck.IsChecked == true)
            {
                chkAllCheckUncheck.Content = "Deselect All";
                chkDev.IsChecked = true;
                chkStage.IsChecked = true;
                chkPreProd.IsChecked = true;
                chkProd.IsChecked = true;
            }
            else
            {
                chkAllCheckUncheck.Content = "Select All";
                chkDev.IsChecked = false;
                chkStage.IsChecked = false;
                chkPreProd.IsChecked = false;
                chkProd.IsChecked = false;
            }
        }

        private void ClearErrorBox_Click(object sender, RoutedEventArgs e)
        {
            ErrorBox.Clear();
        }

        private void chkClientCheckUncheck_Checked(object sender, RoutedEventArgs e)
        {
            if (chkClientCheckUncheck.IsChecked == true)
            {
                chkAllCheckUncheck.Content = "Deselect All";
                chkFirsttrip.IsChecked = true;
                chkTriplover.IsChecked = true;
                chkTravelchamp.IsChecked = true;
                chkTakeoff.IsChecked = true;
                chkTaketrip.IsChecked = true;
            }
            else
            {
                chkAllCheckUncheck.Content = "Select All";
                chkFirsttrip.IsChecked = false;
                chkTriplover.IsChecked = false;
                chkTravelchamp.IsChecked = false;
                chkTakeoff.IsChecked = false;
                chkTaketrip.IsChecked = false;
            }
        }
    }
}