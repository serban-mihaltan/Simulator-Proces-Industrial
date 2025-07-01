using System;
using System.ComponentModel;
using System.Windows.Threading;
using DataModel;
using Communicator;

namespace Simulator
{
    public class SilozViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly int[] levelThresholds = { 100, 200, 300, 400 };
        private readonly int safetyMaxLevel = 400;
        private int selectedLevelIndex;
        private int currentLevel;
        private int lastLevel;
        
        public int CurrentLevel
        {
            get { return currentLevel; }
            set
            {
                //if (currentLevel != value)
                {
                    currentLevel = value;
                    OnPropertyChanged(nameof(CurrentLevel));
                    OnPropertyChanged(nameof(IsNivel1Reached));
                    OnPropertyChanged(nameof(IsNivel2Reached));
                    OnPropertyChanged(nameof(IsNivel3Reached));
                    OnPropertyChanged(nameof(IsNivel4Reached));

                    SendProcessState();
                }
            }
        }

        public int SelectedLevelIndex
        {
            get { return selectedLevelIndex; }
            set
            {
                if (selectedLevelIndex != value)
                {
                    selectedLevelIndex = value;
                    OnPropertyChanged(nameof(SelectedLevelIndex));
                    OnPropertyChanged(nameof(IsNivel1Selected));
                    OnPropertyChanged(nameof(IsNivel2Selected));
                    OnPropertyChanged(nameof(IsNivel3Selected));
                    OnPropertyChanged(nameof(IsNivel4Selected));

                    TheStateOfTheProcess = ProcessState.schimbare_nivel;
                }
            }
        }

        public bool IsNivel1Selected { get { return SelectedLevelIndex == 0; } }
        public bool IsNivel2Selected { get { return SelectedLevelIndex == 1; } }
        public bool IsNivel3Selected { get { return SelectedLevelIndex == 2; } }
        public bool IsNivel4Selected { get { return SelectedLevelIndex == 3; } }

        public bool IsNivel1Reached => CurrentLevel >= levelThresholds[0];
        public bool IsNivel2Reached => CurrentLevel >= levelThresholds[1];
        public bool IsNivel3Reached => CurrentLevel >= levelThresholds[2];
        public bool IsNivel4Reached => CurrentLevel >= levelThresholds[3];

        public int LoadSpeed { get; set; }
        public int DischargeSpeed { get; set; }

        public bool IsAutomaticMode
        {
            get { return isAutomaticMode; }
            set
            {
                if (isAutomaticMode != value)
                {
                    isAutomaticMode = value;
                    OnPropertyChanged(nameof(IsAutomaticMode));
                }
            }
        }
        private bool isAutomaticMode;

        public RelayCommand SetLevelCommand { get; }
        public RelayCommand IncarcareCommand { get; }
        public RelayCommand DescarcareCommand { get; }

        private Sender monitor;
        private ProcessState processState;
        private DispatcherTimer simulationTimer;

        public SilozViewModel()
        {
            LoadSpeed = 10;
            DischargeSpeed = 10;
            SelectedLevelIndex = 0;
            CurrentLevel = levelThresholds[0];
            
            SetLevelCommand = new RelayCommand(param =>
            {
                if (int.TryParse(param.ToString(), out int index))
                {
                    SelectedLevelIndex = index;
                    TheStateOfTheProcess = ProcessState.schimbare_nivel;
                    //SendProcessState();
                }
            });

            IncarcareCommand = new RelayCommand(param =>
            {
                if (CurrentLevel < safetyMaxLevel)
                {
                    CurrentLevel += LoadSpeed;
                    if (CurrentLevel > levelThresholds[SelectedLevelIndex])
                        CurrentLevel = levelThresholds[SelectedLevelIndex];
                    if (CurrentLevel > safetyMaxLevel)
                        CurrentLevel = safetyMaxLevel;
                   // SendProcessState();
                }
            });

            DescarcareCommand = new RelayCommand(param =>
            {
                if (CurrentLevel > 0)
                {
                    CurrentLevel -= DischargeSpeed;
                    if (CurrentLevel < 0)
                        CurrentLevel = 0;
                    //SendProcessState();
                }
            });

            monitor = new Sender("127.0.0.1", 3000);

            simulationTimer = new DispatcherTimer();
            simulationTimer.Interval = TimeSpan.FromMilliseconds(10);
            simulationTimer.Tick += SimulationTick;
            simulationTimer.Start();
        }
        
        private void SimulationTick(object sender, EventArgs e)
        {
            if (IsAutomaticMode)
            {
                if (CurrentLevel < levelThresholds[SelectedLevelIndex])
                {
                    CurrentLevel += LoadSpeed;
                    
                }
                //daca silozul trebuie sa se regleze la nivelul dorit decomenteaza sectiunea de mai jos

               /* if (CurrentLevel > levelThresholds[SelectedLevelIndex])
                {
                        CurrentLevel -= DischargeSpeed;
                }*/
                Reglare();
            }
            //daca se vrea sa se trimita la fiecare tick starea procesului
            //SendProcessState();


        }
        private void Reglare()
        {
            int level = levelThresholds[SelectedLevelIndex];
            if (CurrentLevel > level)
            { 
                if (DischargeSpeed > CurrentLevel - level)
                    CurrentLevel = level;
            } 
            else if (CurrentLevel< level)
            {
                if(LoadSpeed > level-CurrentLevel)
                    CurrentLevel = level;
            }
        }
        private void SendProcessState()
        {
            
            ProcessState newState;
            if (CurrentLevel == lastLevel)
                newState = ProcessState.mentinere;
            else if (CurrentLevel > lastLevel)
                newState = ProcessState.incarcare;
            else if (CurrentLevel <lastLevel)
                newState = ProcessState.descarcare;
            else
                newState = ProcessState.mentinere;

            //if (newState != TheStateOfTheProcess)
                TheStateOfTheProcess = newState;
            if (CurrentLevel < levelThresholds[SelectedLevelIndex])
                lastLevel = CurrentLevel;
            else lastLevel = levelThresholds[SelectedLevelIndex];
        }

        public ProcessState TheStateOfTheProcess
        {
            get { return processState; }
            set
            {
                //if (processState != value )
                {
                    processState = value;
                    monitor?.Send(Convert.ToByte(processState));
                }
            }
        }
    }
}
