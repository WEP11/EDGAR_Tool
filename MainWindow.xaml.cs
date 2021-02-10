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


namespace EDGAR_Tool
{
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
        public MainWindow()
        {
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
            InitializeComponent();
            // Get Metadata
            loadMetadata();

            // Populate Company List
            getCompanyData();
            populateDocumentList();
            // Populate Filter Combo
            createFilterCombo();
        }
        private void removeCompany(string companyName)
        {

        }

        private void addCompany()
        {

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
            newItem.Content = "Last 25 Filings";
            filterCombo.Items.Add(newItem);
            filterCombo.Items.Add("Last 50 Filings");
            for (int i=0; i<FILING_TYPECODES.Count; i++)
            {
                newItem = new ComboBoxItem();
                newItem.Content = FILING_TYPECODES[i].Replace("\"", string.Empty);
                newItem.ToolTip = FILING_TYPEDESCS[i].Replace("\"", string.Empty);
                filterCombo.Items.Add(newItem);
            }
        }

        private void populateDocumentList(string filter = null)
        {
            // Get the selected company CIK
            string selected = companyNames[0];
            int selectedCompany = 0;
            if (companyListBox.SelectedItem != null)
            {
                selected = companyListBox.SelectedItem.ToString();
            }


            for (int i = 0; i < companyCount; i++)
            {
                if (companyNames[i] == selected)
                {
                    selectedCompany = i;
                    break;
                }
            }

            // Get any document filters
            if (filter == null)
            {
                filter = filterCombo.Text;
            }
            switch (filter)
            {
                case "Last 25 Filings":
                case "Last 50 Filings": // Filter is the filter in these cases
                    break;
                default: // Break off first filing code in other cases
                    filter = filter.Replace(" ", string.Empty);
                    filter = filter.Split(',')[0];
                    break;
            }

            // Collect historical cut-off if enabled
            string history = null;
            if (historyEnable.IsChecked ?? false)
            {
                DateTime history_date = dateSelector.SelectedDate ?? DateTime.Now;
                history = history_date.ToString("yyyyMMdd");
            }

            EdgarReader testCompany = new EdgarReader(companyCIKs[selectedCompany], filter, history);
            companyLabel.Content = testCompany.getCompanyName();
            SyndicationFeed companyFeed = testCompany.getFeed();
            
            companyDocList.Items.Clear();
            for (int i=0; i < companyFeed.Items.Count(); i++)
            {
                ListBoxItem docItem = new ListBoxItem();
                docItem.Content = (companyFeed.Items.ElementAt(i).Title.Text + "\n\t" + companyFeed.Items.ElementAt(i).LastUpdatedTime.ToString("dd-MMM yyyy"));
                docItem.Tag = companyFeed.Items.ElementAt(i).Links[0].Uri.OriginalString;
                companyDocList.Items.Add(docItem);
            }
        }

        private void getCompanyData()
        {
            companyListBox.Items.Clear();
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
                    companyListBox.Items.Add(read2["name"]);
                    companyCIKs[i] = read2["cik"];
                    companyNames[i] = read2["name"];
                    companyTicks[i] = read2["ticker"];
                                            
                    i++;
                }
            }
            reader.Close();
            reader.Dispose();
        }

        private void companyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            populateDocumentList();
        }

        private void companyDocList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            openReportButton.IsEnabled = true;
        }

        private void openReportButton_Click(object sender, RoutedEventArgs e)
        {
            string selected_url = (companyDocList.SelectedItem as ListBoxItem).Tag.ToString();
            
            // Get HTML report text from filing
            EdgarFiling selected_document = new EdgarFiling(selected_url);
            string document_text = selected_document.getFilingText();
            string document_title = selected_document.getFilingType() + " - " + selected_document.getFiler();
            ReportWindow viewer = new ReportWindow(document_text, document_title);
            viewer.Show();

        }

        private void openOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow options = new OptionsWindow();
            options.Show();
        }

        private void filterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            populateDocumentList((e.AddedItems[0] as ComboBoxItem).Content as string);
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
            addCompany();
        }
    }
}
