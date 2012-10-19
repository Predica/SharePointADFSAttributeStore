using Predica.Tools.SharePoint.SharePointAttributeStore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharePointAttributeStoreTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var las = new SharePointListAttributeStore();

            las.Initialize(new Dictionary<string, string>() { { "SiteUrl", "https://portal.qsdev.local" }, { "ListName", "Persons" } });

            var asyncResult = las.BeginExecuteQuery(
                "<View><Query><Where><Or><Eq><FieldRef Name='Email'/><Value Type='Text'>{0}</Value></Eq><Eq><FieldRef Name='QExternalId'/><Value Type='Text'>{1}</Value></Eq></Or></Where></Query><ViewFields><FieldRef Name='ID'/></ViewFields><RowLimit>1</RowLimit></View>",
                new string[] { "email@example.com", "password" },
                null,
                null);

            string[][] results = las.EndExecuteQuery(asyncResult);

            if (results.Count() == 0)
            {
                Console.WriteLine("No results");
                Console.ReadLine();
                return;
            }

            foreach (string[] resultrow in results)
            {
                foreach (string result in resultrow)
                {
                    Console.Write(result + " ");
                }

                Console.WriteLine();
            }
            Console.ReadLine();
        }
    }
}
