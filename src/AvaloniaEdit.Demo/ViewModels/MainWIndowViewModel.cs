using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Newtonsoft.Json;
using ReactiveUI;
using TextMateSharp.Grammars;

namespace AvaloniaEdit.Demo.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public async void Execute(object parameter)
        {
            await _execute();
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public class MainWindowViewModel : ReactiveObject
    {
        private string _url= "http://www.baidu.com";
        private string _headers;
        private string _body;
        private string _response = "WELCOME!";
        private string _selectedMethod;
        private ObservableCollection<string> _methods;
        private TextEditor _textEditor;
        private TextMate.TextMate.Installation _textMate;
        private RegistryOptions _themes;
        public MainWindowViewModel(TextMate.TextMate.Installation _textMate, RegistryOptions _themes, TextEditor _textEditor)
        {
            this._textMate = _textMate;
            this._themes = _themes;
            this._textEditor = _textEditor;

            Methods = new ObservableCollection<string> { "GET", "POST", "PUT", "DELETE" };
            SelectedMethod = Methods[0];
            SendRequestCommand = new RelayCommand(async () => await SendRequest());
        }
        public string Response
        {
            get => _response;
            set
            {
                _response = value;
                _textEditor.Document = new TextDocument(value);
                this.RaisePropertyChanged(nameof(Response));
                this.RaisePropertyChanged(nameof(FormattedJsonResponseDocument));
                this.RaisePropertyChanged(nameof(FormattedXmlResponseDocument));
                this.RaisePropertyChanged(nameof(FormattedHtmlResponseDocument));
                
            }
        }
        public string Url
        {
            get => _url;
            set
            {
                _url = value;

                this.RaiseAndSetIfChanged(ref _url, value);
            }
        }

        public string Headers
        {
            get => _headers;
            set
            {
                _headers = value;
                this.RaiseAndSetIfChanged(ref _url, value);
            }
        }

        public string Body
        {
            get => _body;
            set
            {
                _body = value;
                this.RaiseAndSetIfChanged(ref _body, value);
            }
        }
        public TextDocument FormattedJsonResponseDocument
        {
            get
            {
                try
                {
                    var parsedJson = JsonConvert.DeserializeObject(Response);
                    var formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                    return new TextDocument(formattedJson);
                }
                catch
                {
                    return new TextDocument("Invalid JSON");
                }
            }
        }

        public TextDocument FormattedXmlResponseDocument
        {
            get
            {
                try
                {
                    var parsedXml = XDocument.Parse(Response);
                    return new TextDocument(parsedXml.ToString());
                }
                catch
                {
                    return new TextDocument("Invalid XML");
                }
            }
        }

        public TextDocument FormattedHtmlResponseDocument
        {
            get
            {
                // Assuming the response is already in HTML format
                return new TextDocument(Response);
            }
        }

        public string SelectedMethod
        {
            get => _selectedMethod;
            set
            {
                _selectedMethod = value;
                this.RaiseAndSetIfChanged(ref _selectedMethod, value);
            }
        }

        public ObservableCollection<string> Methods
        {
            get => _methods;
            set
            {
                _methods = value;
                this.RaiseAndSetIfChanged(ref _methods, value);
            }
        }

        public ICommand SendRequestCommand { get; }

        private async Task SendRequest()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(SelectedMethod), Url);

                    if (!string.IsNullOrEmpty(Headers))
                    {
                        foreach (var header in Headers.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                        {
                            var headerParts = header.Split(':');
                            if (headerParts.Length == 2)
                            {
                                request.Headers.Add(headerParts[0].Trim(), headerParts[1].Trim());
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(Body) && (SelectedMethod == "POST" || SelectedMethod == "PUT"))
                    {
                        request.Content = new StringContent(Body);
                    }

                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Response = responseBody;
                    });
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Response = $"Error: {ex.Message}";
                    });
                }
            }
        }
    

        public ObservableCollection<ThemeViewModel> AllThemes { get; set; } = [];
        private ThemeViewModel _selectedTheme;

        public ThemeViewModel SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTheme, value);
                _textMate.SetTheme(_themes.LoadTheme(value.ThemeName));
            }
        }

        public void CopyMouseCommand(TextArea textArea)
        {
            ApplicationCommands.Copy.Execute(null, textArea);
        }

        public void CutMouseCommand(TextArea textArea)
        {
            ApplicationCommands.Cut.Execute(null, textArea);
        }

        public void PasteMouseCommand(TextArea textArea)
        {
            ApplicationCommands.Paste.Execute(null, textArea);
        }

        public void SelectAllMouseCommand(TextArea textArea)
        {
            ApplicationCommands.SelectAll.Execute(null, textArea);
        }

        // Undo Status is not given back to disable it's item in ContextFlyout; therefore it's not being used yet.
        public void UndoMouseCommand(TextArea textArea)
        {
            ApplicationCommands.Undo.Execute(null, textArea);
        }

    }
}