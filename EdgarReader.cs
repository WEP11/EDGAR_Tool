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

namespace EDGAR_Tool
{
    public class EdgarReader
    {
        private string company_cik;
        private SyndicationFeed feed;

        public EdgarReader(string cik, string filter, string history) // 0000312070
        {
            string url = "";
            company_cik = cik;
            if (filter == "Last 25 Filings")
            {
                url = "https://www.sec.gov/cgi-bin/browse-edgar?action=getcompany&CIK=" + cik + "&type=&dateb=" + history + "&owner=include&start=0&count=25&output=atom";
            }
            else if (filter == "Last 50 Filings")
            {
                url = "https://www.sec.gov/cgi-bin/browse-edgar?action=getcompany&CIK=" + cik + "&type=&dateb=" + history + "&owner=include&start=0&count=50&output=atom";
            }
            else
            {
                url = "https://www.sec.gov/cgi-bin/browse-edgar?action=getcompany&CIK=" + cik + "&type=" + filter + "&dateb=" + history + "&owner=include&start=0&count=25&output=atom";
            }
            
            XmlReader reader = XmlReader.Create(url);

            feed = SyndicationFeed.Load(reader);
            reader.Close();
        }

        public string getCompanyName()
        {
            /*
            foreach (SyndicationItem item in feed.Items)
            {
                Console.WriteLine(item);
            }*/
            return feed.Title.Text;
        }

        public SyndicationFeed getFeed()
        {
            return feed;
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

        public EdgarFiling(string url)
        {
            document_location = url.Substring(0, url.Length - 10) + ".txt";
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
            // Load filing from URL as Text Stream (there are non-conforming XML components)
            WebRequest webRequest = WebRequest.Create(document_location);
            WebResponse response = webRequest.GetResponse();
            Stream webContent = response.GetResponseStream();

            StreamReader txtReader = new StreamReader(webContent);

            string line;
            int documentCount = 0;
            bool isInsideHeader = false;

            // Get header information
            while ((line = txtReader.ReadLine()) != null)
            {
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

            document_types = new string[documentCount];
            document_text = new string[documentCount];

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
            }

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
