using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RadencyTask1.Classes
{
    public class ETL
    {
        private CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        private CancellationToken CancellationToken { get; set; }
        private Task Task { get; set; }
        private List<City> Cities { get; set; } = new List<City>();
        private int ParsedFiles { get; set; } = 0;
        private int ParsedLines { get; set; } = 0;
        private int FoundErrors { get; set; } = 0;
        private List<string> InvalidFiles { get; set; } = new List<string>();
        private void AddInvalidFilepath(string filepath)
        {
            if (!InvalidFiles.Contains(filepath)) InvalidFiles.Add(filepath);
        }
        private void AddDataRow(DataRow dataRow)
        {
            City city = (from cities in Cities
                        where cities.CityName == dataRow.City
                        select cities).FirstOrDefault();
            if (city == null) 
            {
                city = new City(dataRow.City);
                Cities.Add(city); 
            }

            Service service = (from services in city.Services
                               where services.Name == dataRow.Service
                               select services).FirstOrDefault();
            if(service == null)
            {
                service = new Service(dataRow.Service);
                city.Services.Add(service);
            }

            Payer payer = new Payer { 
                Name = dataRow.Name,
                Date = dataRow.Date,
                Payment = dataRow.Payment,
                AccountNumber = dataRow.AccountNumber
            };
            service.Payers.Add(payer);
            service.Total += payer.Payment;
            city.Total += payer.Payment;
        }
        public void ShowMenu()
        {
            if(IsValidFilepath())
            {
                Console.WriteLine("Menu:");
                Console.WriteLine("1. Start");
                Console.WriteLine("2. Reset");
                Console.WriteLine("3. Stop");
                Console.WriteLine("4. Change filepaths");
                Console.WriteLine("0. Exit");
                Console.Write("Input: ");
                string input = Console.ReadLine();
                Console.WriteLine();
                
                switch (input)
                {
                    case "1":
                        CancellationTokenSource = new CancellationTokenSource();
                        CancellationToken = CancellationTokenSource.Token;

                        Task = new Task(Start, CancellationToken);
                        Task.Start();

                        ShowMenu();
                        break;

                    case "2":
                        Reset();
                        ShowMenu();
                        break;

                    case "3":
                        Stop();
                        ShowMenu();
                        break;

                    case "4":
                        ChangeFilepath();
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Invalid input!");
                        ShowMenu();
                        break;
                }
            } 
            else
            {
                ChangeFilepath();
                ShowMenu();
            }
        }
        private void Start()
        {
            string rawDataFilepath = ConfigurationManager.AppSettings["rawDataFilepath"];
            string doneDataFilepath = ConfigurationManager.AppSettings["doneDataFilepath"];
            CancellationToken = CancellationTokenSource.Token;

            Read();
            SaveLog();

            Console.WriteLine("Reading and writing are finished!");
        }
        private void Stop()
        {
            CancellationTokenSource.Cancel();
        }
        private void Reset()
        {
            Stop();

            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            Task = new Task(Start, CancellationToken);
            Task.Start();
        }
        private void Read()
        {
            string rawDataFilepath = ConfigurationManager.AppSettings["rawDataFilepath"];

            List<string> fileList = Directory.GetFiles(rawDataFilepath)
                .Where(file => file.EndsWith(".txt") || file.EndsWith(".csv"))
                .ToList();
            List<string> processedFilesList = GetProcessedFiles();
            fileList = fileList.Except(processedFilesList).ToList();

            StreamReader streamReader;
            Validator validator;
            DataRow dataRow;
            for (int i = 0; i < fileList.Count; i++)
            {
                streamReader = new StreamReader(fileList[i]);

                string row;
                bool isValidRow;
                if (fileList[i].EndsWith(".csv")) row = streamReader.ReadLine();
                while ((row = streamReader.ReadLine()) != null)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        ClearTemp();
                        streamReader.Close();
                        return;
                    }
                    validator = new Validator(row);

                    dataRow = new DataRow();
                    isValidRow = validator.Validate(ref dataRow);

                    if (isValidRow) AddDataRow(dataRow);
                    else
                    {
                        AddInvalidFilepath(fileList[i]);
                        FoundErrors++;
                    }
                    ParsedLines++;

                    Thread.Sleep(1000);
                }

                streamReader.Close();

                AddProcessedFile(fileList[i]);
                Save();                
                ClearTemp();
            }
        }
        private void AddProcessedFile(string filepath)
        {
            string doneDataFilepath = ConfigurationManager.AppSettings["doneDataFilepath"];

            StreamWriter streamWriter = new StreamWriter($"{doneDataFilepath}\\processedFiles.txt", true);
            streamWriter.WriteLine($"{filepath}");
            streamWriter.Close();

            ParsedFiles++;
        }
        private List<string> GetProcessedFiles()
        {
            List<string> processedFiles = new List<string>();
            string doneDataFilepath = ConfigurationManager.AppSettings["doneDataFilepath"];

            try
            {
                StreamReader streamReader = new StreamReader($"{doneDataFilepath}\\processedFiles.txt");
                string row;
                while ((row = streamReader.ReadLine()) != null)
                {
                    processedFiles.Add(row);
                }
                streamReader.Close();
            } catch (Exception ex) { }            

            return processedFiles;
        }
        private void ClearTemp()
        {
            Cities = new List<City>();
        }
        private void Save()
        {
            string doneDataFilepath = ConfigurationManager.AppSettings["doneDataFilepath"];

            string json = JsonSerializer.Serialize(Cities);
            string date = DateTime.Now.Date.ToShortDateString();
            string outputFilepath = $"{doneDataFilepath}\\{date}";
            Directory.CreateDirectory(outputFilepath);

            outputFilepath += $"\\output{ParsedFiles}.json";

            StreamWriter streamWriter = new StreamWriter(outputFilepath);
            streamWriter.Write(json);
            streamWriter.Close();                      
        }
        private void SaveLog()
        {
            string doneDataFilepath = ConfigurationManager.AppSettings["doneDataFilepath"];
            StreamWriter streamWriter;
            string date = DateTime.Now.ToShortDateString();
            Directory.CreateDirectory($"{doneDataFilepath}\\{date}");

            string metaLogFilepath = $"{doneDataFilepath}\\{date}\\meta.log";
            streamWriter = new StreamWriter(metaLogFilepath);
            string log = $"ParsedFiles: {GetProcessedFiles().Count}\n" +
                $"ParsedLines: {ParsedLines}\n" +
                $"FoundErrors: {FoundErrors}\n" +
                $"InvalidFiles: {InvalidFilepathToString()}";
            streamWriter.Write(log);
            streamWriter.Close();
        }
        private bool IsValidFilepath()
        {
            bool isValidFilepath = false;
            string rawDataFilepath = ConfigurationManager.AppSettings["rawDataFilepath"];
            string doneDataFilepath = ConfigurationManager.AppSettings["doneDataFilepath"];
            if (Directory.Exists(rawDataFilepath) && Directory.Exists(doneDataFilepath)) isValidFilepath = true;

            // Debug
            //Console.WriteLine($"{rawDataFilepath}, {doneDataFilepath}");

            return isValidFilepath;
        }
        private void ChangeFilepath()
        {
            Console.WriteLine("Filepaths are not valid! Please enter new filepaths.");
            Console.Write("Raw data filepath: ");
            string rawDataFilepath = Console.ReadLine();

            Console.WriteLine();
            Console.Write("Done data filepath: ");
            string doneDataFilepath = Console.ReadLine();

            ChangeFilepath(rawDataFilepath, doneDataFilepath);
        }
        private void ChangeFilepath(string rawDataFilepath, string doneDataFilepath)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                settings["rawDataFilepath"].Value = rawDataFilepath;
                settings["doneDataFilepath"].Value = doneDataFilepath;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
        private string InvalidFilepathToString()
        {
            string invalidFilepath = "[";
            for(int i = 0; i < InvalidFiles.Count; i++)
            {
                invalidFilepath += InvalidFiles[i];
                if (i != InvalidFiles.Count - 1) invalidFilepath += ", ";
            }
            invalidFilepath += "]";

            return invalidFilepath;
        }
    }
}
