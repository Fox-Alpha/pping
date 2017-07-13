using GalaSoft.MvvmLight;

namespace codingfreaks.pping.Ui.WindowsApp.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;

    using GalaSoft.MvvmLight.Command;

    using Messages;

    using Models;

    using Newtonsoft.Json;

    /// <summary>
    /// ViewModel for the main view.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
       
        public MainViewModel()
        {
            var appName = Assembly.GetExecutingAssembly().GetName().Name;
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                Title = appName + " (DESIGNER)";
                FillDesignTimeJobs();
            }
            else
            {
                // Code runs "for real"
                Title = appName;
                InitCommands();
                ReloadOptions();
            }
        }

        private void FillDesignTimeJobs()
        {
            Jobs = new BindingList<JobModel>(new List<JobModel>()
            {
                new JobModel()
                {
                    TargetAddess = "google.de",
                    TargetPorts = new [] {80,443},                    
                }
            });
        }

        private void ReloadOptions()
        {
            if (!File.Exists(OptionsFilePath))
            {
                return;
            }
            try
            {
                var fileContent = File.ReadAllText(OptionsFilePath);
                var options = JsonConvert.DeserializeObject<OptionsModel>(fileContent);
                foreach (var job in options.JobDefinitions)
                {
                    Jobs.Add(job);                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void InitCommands()
        {
            AddJobCommand = new RelayCommand(
                () =>
                {
                    MessengerInstance.Send(new AddJobWindowOpenMessage());
                });
            RemoveJobCommand = new RelayCommand(() => { }, () => CurrentSelectedJob != null);
            ClosingCommand = new RelayCommand(
                () =>
                {
                    SaveCommand.Execute(null);
                });
            SaveCommand = new RelayCommand(
                () =>
                {
                    var options = new OptionsModel()
                    {
                        JobDefinitions = Jobs.ToList()
                    };
                    var fileContent = JsonConvert.SerializeObject(options);
                    try
                    {
                        File.WriteAllText(OptionsFilePath, fileContent);
                    }
                    catch (Exception ex)
                    {
                        
                    }
                });
        }

        public string Title { get; private set; }

        public BindingList<JobModel> Jobs { get; set; } = new BindingList<JobModel>();

        public RelayCommand AddJobCommand { get; private set; }

        public RelayCommand RemoveJobCommand { get; private set; }

        public RelayCommand SaveCommand { get; private set; }

        public RelayCommand ClosingCommand { get; private set; }

        private readonly string OptionsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "options.json");

        public JobModel CurrentSelectedJob { get; set; }
    }
}