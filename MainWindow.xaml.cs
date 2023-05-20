using System;
using System.IO;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using Newtonsoft.Json;

namespace EDGAR_Tool
{
    public class EdgarDefinition
    {
        public string cik_str { get; set; }
        public string ticker { get; set; }
        public string title { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<String> FILING_TYPECODES = new List<string>();
        private List<String> FILING_TYPEDESCS = new List<string>();

        private string[] companyCIKs;
        private string[] companyNames;
        private string[] companyTicks;
        private int companyCount;

        private EdgarReader selectedCompany;

        public MainWindow()
        {
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;

            AboutWindow aboutDialog = new AboutWindow();
            aboutDialog.Show();

            InitializeComponent();
            // Get Metadata
            loadMetadata();

            // Populate Company List
            getCompanyData();
            loadCompanyList();
            selectCompany();
            
            // Populate Filter Combo
            createFilterCombo();
        }
        private void removeCompany(string companyName)
        {
            int removeIndex = -1;
            int newSize = companyTicks.Length;
            string[] newCIKs = new string[newSize];
            string[] newNames = new string[newSize];
            string[] newTicks = new string[newSize];

            int count = 0;
            for(int i=0; i< companyNames.Length; i++)
            {
                if (companyNames[i] == companyName)
                {
                    removeIndex = i;
                    continue;
                }
                else
                {
                    newCIKs[count] = companyCIKs[i];
                    newNames[count] = companyNames[i];
                    newTicks[count] = companyTicks[i];
                    count += 1;
                }
            }
            if (removeIndex > 0)
            {
                companyNames = newNames;
                companyCIKs = newCIKs;
                companyTicks = newTicks;
                loadCompanyList();
            }
        }

        private void loadMetadata()
        {
            StreamReader reader = new StreamReader("filing-types.txt");
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(';');
                FILING_TYPECODES.Add(values[0]);
                FILING_TYPEDESCS.Add(values[1]);
            }
        }

        private void createFilterCombo()
        {
            ComboBoxItem newItem = new ComboBoxItem();
            newItem.IsSelected = true;
            newItem.Content = "Any";
            filterCombo.Items.Add(newItem);
            for (int i=0; i<FILING_TYPECODES.Count; i++)
            {
                newItem = new ComboBoxItem();
                newItem.Content = FILING_TYPECODES[i].Replace("\"", string.Empty);
                newItem.ToolTip = FILING_TYPEDESCS[i].Replace("\"", string.Empty);
                filterCombo.Items.Add(newItem);
            }

            newItem = new ComboBoxItem();
            newItem.IsSelected = true;
            newItem.Content = "Last 25 Filings";
            limitCombo.Items.Add(newItem);
            limitCombo.Items.Add("Last 50 Filings");
            limitCombo.Items.Add("Last 100 Filings");
            limitCombo.Items.Add("Last 250 Filings");
            limitCombo.Items.Add("All Available");
        }

        private void selectCompany()
        {
            // Get the selected company CIK
            string selected = companyNames[0];
            int companyIndex = 0;
            if (companyListBox.SelectedItem != null)
            {
                selected = companyListBox.SelectedItem.ToString();
            }

            for (int i = 0; i < companyNames.Length; i++)
            {
                if (companyNames[i] == selected)
                {
                    companyIndex = i;
                    break;
                }
            }

            selectedCompany = new EdgarReader(companyCIKs[companyIndex]);
            companyLabel.Content = selectedCompany.getCompanyName();
            populateDocumentList();
        }

        private void populateDocumentList(string filter = null, string limit = null)
        {
            int fileLimit = 25;
            // Set list limit
            if (limit != null)
            {
                switch(limit)
                {
                    case "Last 25 Filings":
                        fileLimit = 25;
                        break;
                    case "Last 50 Filings":
                        fileLimit = 50;
                        break;
                    case "Last 100 Filings":
                        fileLimit = 100;
                        break;
                    case "Last 250 Filings":
                        fileLimit = 250;
                        break;
                    case "All Available":
                        fileLimit = 0;
                        break;
                }
            }

            // Set filter
            if (filter == null)
            {
                filter = filterCombo.Text;
            }
            if (filter != "Any")
            {
                filter = filter.Replace(" ", string.Empty);
                filter = filter.Split(',')[0];
            }
            

            // Collect historical cut-off if enabled
            string history = null;
            if (historyEnable.IsChecked ?? false)
            {
                DateTime history_date = dateSelector.SelectedDate ?? DateTime.Now;
                history = history_date.ToString("yyyyMMdd");
            }

            dynamic companyFeed = selectedCompany.getFeed();

            companyDocList.Items.Clear();

            int fileCount = 0;
            for (int i=0; i < companyFeed.filings.recent.accessionNumber.Count; i++)
            {
                // Filter the data if needed
                bool createFiling = false;
                if (filter != null)
                {
                    if (companyFeed.filings.recent.form[i].Contains(filter))
                    {
                        createFiling = true;
                    }
                    else if (filter == "Any")
                    {
                        createFiling = true;
                    }
                }
                else
                {
                    createFiling = true;
                }
                
                // Add filing to list
                if (createFiling)
                {
                    string filingDesc = "";
                    
                    for (int j=0; j < FILING_TYPECODES.Count; j++)
                    {
                        if (companyFeed.filings.recent.form[i].Contains(FILING_TYPECODES[j].Split(',')[0]) || 
                            companyFeed.filings.recent.form[i] == FILING_TYPECODES[j].Split(',')[0])
                        {
                            filingDesc = FILING_TYPEDESCS[j];
                        }
                    }

                    ListBoxItem docItem = new ListBoxItem();
                    docItem.Content = (companyFeed.filings.recent.form[i] + " - " + filingDesc + "\n\t" + companyFeed.filings.recent.filingDate[i]);
                    string documentUrl = "https://www.sec.gov/Archives/edgar/data/" + companyFeed.cik + "/" + companyFeed.filings.recent.accessionNumber[i].Replace("-", "") + "/" + companyFeed.filings.recent.primaryDocument[i];
                    docItem.Tag = documentUrl;
                    companyDocList.Items.Add(docItem);
                    fileCount += 1;
                }
                if (fileCount >= fileLimit) 
                {
                    break;
                }
            }
        }

        private bool addCompanyToList(string ticker)
        {
            // Get CIK information
            string company_name = null;
            string company_cik = null;
            string url = "https://www.sec.gov/files/company_tickers.json";
            // Load filing from URL as Text Stream (there are non-conforming XML components)
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", "Warren Pettee wepettee@gmail.com");
            string json_content = client.DownloadString(url);
            List<EdgarDefinition> secLookup = JsonConvert.DeserializeObject<List<EdgarDefinition>>(json_content);


            Console.WriteLine(company_cik);
            Console.WriteLine(company_name);
            Console.WriteLine(ticker);

            int newSize = companyTicks.Length + 1;
            string[] newCIKs = new string[newSize];
            string[] newNames = new string[newSize];
            string[] newTicks = new string[newSize];

            int count = 0;
            for (int i = 0; i < companyNames.Length; i++)
            {
                newCIKs[count] = companyCIKs[i];
                newNames[count] = companyNames[i];
                newTicks[count] = companyTicks[i];
                count += 1;
            }
            newCIKs[newSize - 1] = company_cik;
            newNames[newSize - 1] = company_name;
            newTicks[newSize - 1] = ticker;

            companyNames = newNames;
            companyCIKs = newCIKs;
            companyTicks = newTicks;

            // Reload company information
            loadCompanyList();

            return true;
        }

        private void getCompanyData()
        {
            // Check for company data file
            // If it doesn't exist, make a sample file
            if (!File.Exists("company_data.xml"))
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineOnAttributes = true;
                XmlWriter writer = XmlWriter.Create("company_data.xml", settings);
                writer.WriteStartElement("company");
                writer.WriteAttributeString("cik", "0000312070");
                writer.WriteAttributeString("name", "Barclays PLC");
                writer.WriteAttributeString("ticker", "BCS");
                writer.WriteEndElement();

                writer.Close();
            }
            // Count the companies in the file
            XmlReader reader = XmlReader.Create("company_data.xml");
            companyCount = 0;
            while (reader.Read())
            {
                if (reader.Name == "company")
                {
                    companyCount++;
                }
            }
            reader.Close();
            reader.Dispose();

            companyCIKs = new string[companyCount];
            companyNames = new string[companyCount];
            companyTicks = new string[companyCount];

            // Load the companies from the file
            XmlReader read2 = XmlReader.Create("company_data.xml");
            int i = 0;
            while (read2.Read())
            {
                if (read2.Name == "company")
                {
                    companyCIKs[i] = read2["cik"];
                    companyNames[i] = read2["name"];
                    companyTicks[i] = read2["ticker"];
                                            
                    i++;
                }
            }
            reader.Close();
            reader.Dispose();
        }

        private void loadCompanyList()
        {
            companyListBox.Items.Clear();
            for (int i=0; i < companyNames.Length; i++)
            {
                companyListBox.Items.Add(companyNames[i]);
            }
        }

        private void companyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectCompany();
        }

        private void companyDocList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            openReportButton.IsEnabled = true;
        }

        private void openReportButton_Click(object sender, RoutedEventArgs e)
        {
            string selected_url = (companyDocList.SelectedItem as ListBoxItem).Tag.ToString();
            Console.WriteLine(selected_url);
            // Get HTML report text from filing
            EdgarFiling selected_document = new EdgarFiling(selected_url);
            string document_text = selected_document.getPrimaryFilingText();
            string document_title = "Filing Viewer";//selected_document.getFilingType() + " - " + selected_document.getFiler();
            ReportWindow viewer = new ReportWindow(selected_url, document_title);
            viewer.Show();

        }

        private void openOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow options = new OptionsWindow();
            options.Show();
        }

        private void filterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            populateDocumentList((e.AddedItems[0] as ComboBoxItem).Content as string, limitCombo.Text);
        }

        private void limitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            populateDocumentList(filterCombo.Text, limitCombo.Text);
        }

        private void historyEnable_Checked(object sender, RoutedEventArgs e)
        {
            if (historyEnable.IsChecked ?? false)
            {
                dateSelector.IsEnabled = true;
            }
            else
            {
                dateSelector.IsEnabled = false;
            }
            
        }

        private void removeCompanyButton_Click(object sender, RoutedEventArgs e)
        {
            string selected;
            if (companyListBox.SelectedItem != null)
            {
                selected = companyListBox.SelectedItem.ToString();
                removeCompany(selected);
            }
            else
            {
                MessageBox.Show("Select a company to remove from the list", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            
        }

        private void addCompanyButton_Click(object sender, RoutedEventArgs e)
        {
            bool success = true;
            string ticker = tickerTextBox.Text;
            if (ticker != null)
            {
                progressBar.IsIndeterminate = true;
                progressBar.Visibility = Visibility.Visible;
                success = addCompanyToList(ticker);
                progressBar.Visibility = Visibility.Collapsed;
            }   
            else
            {
                MessageBox.Show("Enter a valid ticker symbol in the text box", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            if (!success)
            {
                MessageBox.Show("Stock symbol does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }
    }
}
