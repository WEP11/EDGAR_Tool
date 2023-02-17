using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace EDGAR_Tool
{
    class EdgarSubmissions
    {
        public string cik { get; set; }
        public string entityType { get; set; }
        public string sic { get; set; }
        public string sicDescription { get; set; }
        public int insiderTransactionForOwnerExists { get; set; }
        public int insiderTransactionForIssuerExists { get; set; }
        public string name { get; set; }
        public List<string> tickers { get; set; }
        public List<string> exchanges { get; set; }
        public string ein { get; set; }
        public string description { get; set; }
        public string website { get; set; }
        public string investorWebsite { get; set; }
        public string category { get; set; }
        public string fiscalYearEnd { get; set; }
        public string stateOfIncorporation { get; set; }
        public string stateOfIncorporationDescription { get; set; }
        public EdgarSubmissions_addresses addresses { get; set; }
        public string phone { get; set; }
        public string flags { get; set; }
        public List<EdgarSubmissions_formerName> formerNames { get; set; }
        public EdgarSubmissions_filings filings { get; set; }

    }

    class EdgarSubmissions_addresses
    {
        public EdgarSubmissions_addresses_address mailing { get; set; }
        public EdgarSubmissions_addresses_address business { get; set; }
    }

    class EdgarSubmissions_addresses_address
    {
        public string street1 { get; set; }
        public string street2 { get; set; }
        public string city { get; set; }
        public string stateOrCountry { get; set; }
        public string zipCode { get; set; }
        public string stateOrCountryDescription { get; set; }
    }

    class EdgarSubmissions_formerName
    {
        public string name { get; set; }
        public string from { get; set; }
        public string to { get; set; }
    }

    class EdgarSubmissions_filings
    {
        public EdgarSubmissions_filings_recent recent { get; set; }
        public List<EdgarSubmissions_filings_file> files { get; set; }
    }

    class EdgarSubmissions_filings_recent
    {
        public List<string> accessionNumber { get; set; }
        public List<string> filingDate { get; set; }
        public List<string> reportDate { get; set; }
        public List<string> acceptanceDateTime { get; set; }
        public List<string> act { get; set; }
        public List<string> form { get; set; }
        public List<string> fileNumber { get; set; }
        public List<string> filmNumber { get; set; }
        public List<string> items { get; set; }
        public List<int> size { get; set; }
        public List<int> isXBRL { get; set; }
        public List<int> isInlineXBRL { get; set; }
        public List<string> primaryDocument { get; set; }
        public List<string> primaryDocDescription { get; set; }

    }

    class EdgarSubmissions_filings_file
    {
        public string name { get; set; }
        public int filingCount { get; set; }
        public string filingFrom { get; set; }
        public string filingTo { get; set; }
    }

    public class EdgarReader
    {
        private string company_cik;
        private EdgarSubmissions filingData;

        public EdgarReader(string cik) // 0000312070
        {
            string url = "";
            company_cik = cik;
            url = "https://data.sec.gov/submissions/CIK" + cik + ".json";
            Console.WriteLine(url);
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", "Warren Pettee wepettee@gmail.com");
            string json = client.DownloadString(url);
            filingData = JsonConvert.DeserializeObject<EdgarSubmissions>(json);

        }

        public string getCompanyName()
        {
            return filingData.name;
        }

        public dynamic getFeed()
        {
            return filingData;
        }
    }

    public class EdgarFiling
    {
        public List<String> FILING_TYPECODES;
        public List<String> FILING_TYPEDESCS;
        private string document_location;
        private string document_filer;

        private string[] document_types;
        private string[] document_text;
        private string primary_text;
        private string primary_type;

        public EdgarFiling(string url)
        {
            document_location = url;
            //loadMetadata();
            loadFiling();
        }

        private void loadMetadata()
        {
            StreamReader reader = new StreamReader("filing-types.txt");
            while(!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(';');
                FILING_TYPECODES.Add(values[0]);
                FILING_TYPEDESCS.Add(values[1]);
            }
        }

        private void loadGaapFiling()
        {

        }

        private string filingTypeFromDoc(string input)
        {
            string output = input;             
 
            return output;
        }

        private void loadFiling()
        {
            // Identify filing data type
            string[] fileSplit = document_location.Split('/');
            string filingDir = fileSplit[fileSplit.Length - 2];
            string fllingNum = fileSplit[fileSplit.Length - 1].Split('.')[0];

            // Load the primary filing
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", "Warren Pettee wepettee@gmail.com");
            Stream webContent = client.OpenRead(document_location);

            StreamReader txtReader = new StreamReader(webContent);

            string line;
            string docText = "";
            // Read and display lines from the file until the end of
            // the file is reached. Load each document into a respective array
            while ((line = txtReader.ReadLine()) != null)
            {
                docText += line + "\n";
            }

            primary_text = docText;

            /*
            string line;
            int documentCount = 0;
            bool isInsideHeader = false;

            if (fileExt == "txt")
            {
                // Get header information
                while ((line = txtReader.ReadLine()) != null)
                {
                    System.Console.WriteLine(line);
                    // Save document HTML document portion
                    if (line.Contains("<SEC-HEADER>"))
                    {
                        isInsideHeader = true;
                    }

                    // Extract document filer
                    if (isInsideHeader && line.Contains("COMPANY CONFORMED NAME:"))
                    {
                        document_filer = line.Substring(25);
                    }

                    // Extract document count
                    if (isInsideHeader && line.Contains("PUBLIC DOCUMENT COUNT:"))
                    {
                        documentCount = Int16.Parse(line.Substring(22));
                    }

                    // Pause file reading when header is done
                    if (line == "</SEC-HEADER>")
                    {
                        isInsideHeader = false;
                        break;
                    }
                }
                document_types = new string[documentCount + 10];
                document_text = new string[documentCount + 10];
            }
           
            string docType = "";
            string docText = "";
            int sequence = 0;
            bool isInsideHtml = false;
            bool isNotFormatted = true;
            // Read and display lines from the file until the end of
            // the file is reached. Load each document into a respective array
            while ((line = txtReader.ReadLine()) != null)
            {
                // Extract document sequence
                
                if (line.Contains("<SEQUENCE>"))
                {
                    sequence = Int16.Parse(line.Substring(10));
                    document_types[sequence-1] = docType;
                }
                // Extract document type
                if (line.Contains("<TYPE>"))
                {
                    docType = filingTypeFromDoc(line.Substring(6));
                }

                // Save document TEXT document portion
                
                if (line == "</TEXT>")
                {
                    if (isNotFormatted)
                    {
                        docText = "<pre>" + docText + "</pre>";
                    }
                    isInsideHtml = false;
                    document_text[sequence-1] = docText;
                    docText = "";
                    isNotFormatted = true;
                }

                if (line == "<HTML>")
                {
                    isNotFormatted = false;
                }

                if (isInsideHtml)
                {
                    docText += line + "\n";
                }

                if (line == "<TEXT>")
                {
                    isInsideHtml = true;
                }
            }*/

            // Lastly, reintegrate images into the main document
            /*
            for (int i=0; i<documentCount; i++)
            {
                if (document_types[i] == "GRAPHIC")
                {
                    // Get image data
                    string image_name = document_text[i].Substring(0, document_text[i].IndexOf(".jpg") + 4).Split(' ').Last();
                    string image_data = "data:image/jpeg;base64," + document_text[i].Substring(document_text[i].IndexOf(image_name) + image_name.Length);
                    image_data = Regex.Replace(image_data, @"\s+", "");
                    image_data = image_data.Substring(0, image_data.IndexOf("end"));
                    string newDocText = document_text[0].Replace(image_name, image_data);
                    document_text[0] = newDocText;
                    // Replace image occurances in text with the data
                }
            }*/
}

        public string getPrimaryFilingText()
        {
            return primary_text;
        }

        public string getFilingText()
        {
            
            return document_text[0];
        }

        public string getFilingType()
        {
            return document_types[0];
        }

        public string getFiler()
        {
            return document_filer;
        }
    }
}
