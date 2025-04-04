using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using Newtonsoft.Json; // Ensure you have installed the Newtonsoft.Json NuGet package

namespace ConsoleApp1
{
    public class Submission
    {
        // Update these URLs to point to your GitHub Pages location.
        public static string xmlURL = "https://<your-github-username>.github.io/HotelDirectory/Hotels.xml";
        public static string xmlErrorURL = "https://<your-github-username>.github.io/HotelDirectory/HotelsErrors.xml";
        public static string xsdURL = "https://<your-github-username>.github.io/HotelDirectory/Hotels.xsd";

        public static void Main(string[] args)
        {
            // 1) Verify the valid XML.
            string result = Verification(xmlURL, xsdURL);
            Console.WriteLine("Valid XML Verification Result:");
            Console.WriteLine(result);
            Console.WriteLine();

            // 2) Verify the XML with injected errors.
            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine("Error XML Verification Result:");
            Console.WriteLine(result);
            Console.WriteLine();

            // 3) Convert the valid XML to JSON.
            result = Xml2Json(xmlURL);
            Console.WriteLine("Converted JSON:");
            Console.WriteLine(result);
        }

        // Q2.1: Validate XML against XSD.
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            var errors = new List<string>();

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add(null, xsdUrl);
                settings.ValidationType = ValidationType.Schema;

                // Collect all validation errors.
                settings.ValidationEventHandler += (sender, e) =>
                {
                    errors.Add($"Line {e.Exception.LineNumber}, Pos {e.Exception.LinePosition}: {e.Message}");
                };

                // Perform the validation read.
                using (XmlReader reader = XmlReader.Create(xmlUrl, settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (Exception ex)
            {
                // Capture any critical errors.
                errors.Add(ex.Message);
            }

            return errors.Count == 0 ? "No Error" : string.Join("\n", errors);
        }

        // Q2.2: Convert the valid XML to JSON.
        public static string Xml2Json(string xmlUrl)
        {
            // Load the XML document.
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlUrl);

            // Select all <Hotel> elements under <Hotels>.
            XmlNodeList hotelNodes = doc.DocumentElement.SelectNodes("Hotel");
            var hotelList = new List<Dictionary<string, object>>();

            foreach (XmlNode hotelNode in hotelNodes)
            {
                var hotelObj = new Dictionary<string, object>();

                // Process Name.
                XmlNode nameNode = hotelNode.SelectSingleNode("Name");
                if (nameNode != null)
                    hotelObj["Name"] = nameNode.InnerText;

                // Process Phone elements.
                var phoneNodes = hotelNode.SelectNodes("Phone");
                var phoneList = new List<string>();
                foreach (XmlNode phone in phoneNodes)
                    phoneList.Add(phone.InnerText);
                hotelObj["Phone"] = phoneList;

                // Process Address.
                XmlNode addressNode = hotelNode.SelectSingleNode("Address");
                if (addressNode != null)
                {
                    var addressObj = new Dictionary<string, object>
                    {
                        ["Number"] = addressNode.SelectSingleNode("Number")?.InnerText,
                        ["Street"] = addressNode.SelectSingleNode("Street")?.InnerText,
                        ["City"] = addressNode.SelectSingleNode("City")?.InnerText,
                        ["State"] = addressNode.SelectSingleNode("State")?.InnerText,
                        ["Zip"] = addressNode.SelectSingleNode("Zip")?.InnerText
                    };

                    // Convert the required attribute to _NearestAirport.
                    XmlAttribute nearestAirportAttr = addressNode.Attributes["NearestAirport"];
                    if (nearestAirportAttr != null)
                        addressObj["_NearestAirport"] = nearestAirportAttr.Value;

                    hotelObj["Address"] = addressObj;
                }

                // Process optional Rating attribute.
                XmlAttribute ratingAttr = hotelNode.Attributes["Rating"];
                if (ratingAttr != null)
                    hotelObj["_Rating"] = ratingAttr.Value;

                hotelList.Add(hotelObj);
            }

            // Build the final JSON structure: { "Hotels": { "Hotel": [ ... ] } }.
            var rootObj = new Dictionary<string, object>();
            var hotelsObj = new Dictionary<string, object>
            {
                ["Hotel"] = hotelList
            };
            rootObj["Hotels"] = hotelsObj;

            return JsonConvert.SerializeObject(rootObj, Formatting.Indented);
        }
    }
}