using System.IO;
using System.Windows;
using Microsoft.Win32;
using SpeechDemo.ViewModel;

namespace SpeechDemo.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ConsoleBox.TextChanged += delegate { ConsoleBox.ScrollToEnd(); };
            ChoiceBox.TextChanged += delegate { ChoiceBox.ScrollToEnd(); };
        }

        private void TranscribeButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Audio"),
                Filter = "Wave File (*.wav)|*.wav"
            };
            if (dialog.ShowDialog() != true) return;
            var currentContext = DataContext as RecognitionContext;
            if (currentContext == null) return;
            currentContext.AudioFile = dialog.FileName;
            currentContext.StartRecognitionMultiThread();
        }
    }
}
