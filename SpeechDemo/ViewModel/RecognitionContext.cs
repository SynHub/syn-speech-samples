using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Commands;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using SpeechDemo.Model;
using Syn.Logging;
using Syn.Speech.Api;

namespace SpeechDemo.ViewModel
{
    public class RecognitionContext : ViewModelBase
    {
        #region Fields
        private Configuration _configuration;
        private StreamSpeechRecognizer _recognizer;
        private static readonly Dictionary<Guid, SynWatch> LateHash = new Dictionary<Guid, SynWatch>();
        private readonly Guid _theGuid = Guid.NewGuid();
        private WaveIn _waveSource;
        private WaveFileWriter _waveFile;
        private readonly DispatcherTimer _micTimer;
        private readonly MMDevice _micDevice;
        private Stream _collectedData;
        private bool _canLoadGrammar;
        private string _listenerString;

        private string _transcribeStatus;
        private float _micLevel;
        private string _choices;
        private bool _canLoadModels;
        private bool _canTranscribe;
        private string _audioFile;
        private string _micLevelString;
        private bool _canListen;
        private string _grammarStatus;

        #endregion

        public RecognitionContext()
        {
            try
            {
                Choices = "Press 'Load Grammar' and then click on 'Start Listening'" +
            " and speak out any of the following \n\n1. One million dollars\n2. How are you\n3. Hello there" +
            "\n4. You are welcome\n5. Happy New Year\n\n Once you'v spoken the sentence click on 'Stop Listening'";

                ListenerString = "Start Listening";
                Logger.LogReceived += LoggerMessageReceeived;
                CanLoadModels = true;
                CanLoadGrammar = true;

                LoadModelCommand = new DelegateCommand(LoadModelsMultiThread);
                LoadGrammarCommand = new DelegateCommand(LoadGrammarMultiThread);
                StartRecognitionCommand = new DelegateCommand(StartRecognitionMultiThread);
                StartListeningCommand = new DelegateCommand(StartListening);


                _micTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1) };
                _micTimer.Tick += micTimer_Elapsed;
                _micTimer.Start();
            }
            catch (Exception exception)
            {
                this.LogError(exception);
            }
        }

        #region Properties

        public string TranscribeStatus
        {
            get { return _transcribeStatus; }
            set
            {
                _transcribeStatus = value;
                OnPropertyChanged("TranscribeStatus");
            }
        }

        public string GrammarStatus
        {
            get { return _grammarStatus; }
            set
            {
                _grammarStatus = value; 
                OnPropertyChanged("GrammarStatus");
            }

        }

        public string MicLevelString
        {
            get { return _micLevelString; }
            set
            {
                _micLevelString = value;
                OnPropertyChanged("MicLevelString");
            }
        }

        public float MicLevel
        {
            get { return _micLevel; }
            set
            {
                _micLevel = value;
                MicLevelString = string.Format("Mic Level: {0}", _micLevel);
                OnPropertyChanged("MicLevel");
            }
        }

        public string ListenerString
        {
            get { return _listenerString; }
            set
            {
                _listenerString = value;
                OnPropertyChanged("ListenerString");
            }
        }

        public string Choices
        {
            get { return _choices; }
            set
            {
                _choices = value;
                OnPropertyChanged("Choices");
            }
        }

        public bool CanLoadModels
        {
            get { return _canLoadModels; }
            set
            {
                _canLoadModels = value;
                OnPropertyChanged("CanLoadModels");
            }
        }

        public bool CanLoadGrammar
        {
            get { return _canLoadGrammar; }
            set
            {
                _canLoadGrammar = value;
                OnPropertyChanged("CanLoadGrammar");
            }
        }

        public bool CanTranscribe
        {
            get { return _canTranscribe; }
            set
            {
                _canTranscribe = value;
                OnPropertyChanged("CanTranscribe");
            }
        }

        public bool CanListen
        {
            get { return _canListen; }
            set
            {
                _canListen = value;
                OnPropertyChanged("CanListen");
            }
        }

        public string AudioFile
        {
            get { return _audioFile; }
            set
            {
                _audioFile = value;
                OnPropertyChanged("AudioFile");
            }
        }

        #endregion

        #region Commands

        public void LoadGrammarMultiThread()
        {
            var thread = new Thread(LoadGrammar);
            thread.Start();
        }
        public void LoadGrammar()
        {
            try
            {
                CanLoadGrammar = false;
                CanLoadModels = true;

                var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "Models");
                var dictionaryPath = Path.Combine(modelPath, "cmudict-en-us.dict");
                var languageModelPath = Path.Combine(modelPath, "en-us.lm.dmp");
                var configuration = new Configuration
                {
                    AcousticModelPath = modelPath,
                    DictionaryPath = dictionaryPath,
                    LanguageModelPath = languageModelPath,
                    UseGrammar = true,
                    GrammarPath = "Models",
                    GrammarName = "hello"
                };
                _recognizer = new StreamSpeechRecognizer(configuration);

                CanListen = true;
                CanTranscribe = false;
            }
            catch (Exception exception)
            {
                this.LogError(exception);
            }
        }
        public DelegateCommand LoadGrammarCommand { get; set; }


        public void LoadModelsMultiThread()
        {
            var thread = new Thread(LoadModels);
            thread.Start();
        }
        public void LoadModels()
        {
            try
            {
                CanLoadModels = false;
                CanLoadGrammar = true;

                var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "Models");
                var dictionaryPath = Path.Combine(modelPath, "cmudict-en-us.dict");
                var languageModelPath = Path.Combine(modelPath, "en-us.lm.dmp");
                var configuration = new Configuration
                {
                    AcousticModelPath = modelPath,
                    DictionaryPath = dictionaryPath,
                    LanguageModelPath = languageModelPath
                };
                _recognizer = new StreamSpeechRecognizer(configuration);

                CanTranscribe = true;
                CanListen = false;
            }
            catch (Exception exception)
            {
                this.LogError(exception);
            }
        }
        public DelegateCommand LoadModelCommand { get; set; }

        public void StartRecognitionMultiThread()
        {
            var thread = new Thread(() => StartRecognition(new FileStream(AudioFile, FileMode.Open)));
            thread.Start();
        }

        public void StartRecognition(Stream stream)
        {
            try
            {
                CanTranscribe = false;
                _recognizer.StartRecognition(stream);
                var result = _recognizer.GetResult();
                _recognizer.StopRecognition();
                if (result != null)
                {
                    MessageBox.Show(result.GetHypothesis());
                }
                stream.Close();
                CanTranscribe = true;
            }
            catch (Exception exception)
            {
                this.LogError(exception);
            }
        }
        public DelegateCommand StartRecognitionCommand { get; set; }

        public void StartListening()
        {
            try
            {
                if (ListenerString == "Start Listening")
                {
                    _waveSource = new WaveIn { WaveFormat = new WaveFormat(16000, 1) };
                    _waveSource.DataAvailable += waveSource_DataAvailable;
                    _waveSource.RecordingStopped += waveSource_RecordingStopped;
                    _collectedData = new MemoryStream();
                    _waveFile = new WaveFileWriter(_collectedData, _waveSource.WaveFormat);
                    _waveSource.StartRecording();
                    ListenerString = "Stop Listening";
                }
                else if (ListenerString == "Stop Listening")
                {
                    _waveSource.StopRecording();
                    ListenerString = "Start Listening";
                }
            }
            catch (Exception exception)
            {
               this.LogError(exception);
               this.LogInfo("PLEASE CHECK IF MIC IS CONNECTED.");
            }
            
        }
        public DelegateCommand StartListeningCommand { get; set; }

        #endregion

        #region Helper Methods/Events

        private void micTimer_Elapsed(object sender, EventArgs e)
        {
            try
            {
                MicLevel  = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia).AudioMeterInformation.MasterPeakValue * 1000;
            }
            catch (Exception)
            {
                //this.LogError(exception);
            }
        }

        private void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_waveFile != null)
            {
                _waveFile.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        private void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            try
            {
                if (_waveSource != null)
                {
                    _waveSource.Dispose();
                    _waveSource = null;
                }

                _collectedData.Position = 0;

                var fileStream = new FileStream("speech.wav", FileMode.Create);

                _collectedData.CopyTo(fileStream);
                _collectedData.Position = 0;


                if (fileStream.Length > 1000)
                {
                    CanListen = false;
                    _recognizer.StartRecognition(_collectedData);
                    var result = _recognizer.GetResult();
                    _recognizer.StopRecognition();
                    if (result != null)
                    {
                        MessageBox.Show(result.GetHypothesis());
                    }
                    _collectedData.Close();
                }

                fileStream.Close();

                CanListen = true;
            }
            catch (Exception exception)
            {
                this.LogError(exception);
            }
        }

        private void LoggerMessageReceeived(object sender, LogReceivedEventArgs e)
        {
            if (!CanLoadModels)
            {
                TranscribeStatus = TranscribeStatus + Environment.NewLine + e.Message;
            }
            else if (!CanLoadGrammar)
            {
                GrammarStatus = GrammarStatus + Environment.NewLine + e.Message;
            }
           
            if (e.Type == LogType.Error)
            {
                if (!CanListen) CanLoadGrammar = true;
                if (!CanTranscribe) CanLoadModels = true;
            }
        }
        #endregion
    }
}
