using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using XLabApp.Infrastructure.Commands;
using XLabApp.Models;
using XLabApp.Services.Interfaces;
using XLabApp.ViewModels.Base;

namespace XLabApp.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        private readonly IDataService _DataService;

        #region Title : string - Заголовок окна

        /// <summary>Заголовок окна</summary>
        private string _Title = "Главное окно";

        /// <summary>Заголовок окна</summary>
        public string Title { get => _Title; set => Set(ref _Title, value); }

        #endregion

        #region Status : string - Статус

        private string _Status = "Готов!";

        public string Status { get => _Status; set => Set(ref _Status, value); }

        #endregion

        private TokenResponse _TokenResponse;

        public string CurrentToken { get => _TokenResponse.access_token; set => _TokenResponse.access_token = value; }

        public string RefreshToken { get => _TokenResponse.refresh_token; set => _TokenResponse.refresh_token = value; }

        public string TokenData { get => _TokenData; set => Set(ref _TokenData, value); }

        private string _TokenData = "";

        public string Login
        {
            get => _Login;
            set
            {
                if (!IsValidLogin(value))
                {
                    MessageBox.Show("Login должен содержать только буквы английского алфавита и символы.");
                    return;
                }
                Set(ref _Login, value);
            }
        }

        private string _Login = "";

        private bool IsValidLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return false;
            var regex = new Regex("^[a-zA-Z0-9_@.-]+$");
            return regex.IsMatch(login);
        }

        private string _Password = "";

        public string Password
        {
            get => _Password;
            set
            {
                if (!IsValidPassword(value))
                {
                    MessageBox.Show("Пароль должен быть длиной не менее 8 символов и содержать как минимум одну заглавную букву, одну строчную букву и одну цифру. Поддерживается только английский алфовит", "Неправильный формат пароля", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Set(ref _Password, value);
            }
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            var regex = new Regex("^[a-zA-Z0-9!@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?`~]+$");
            return regex.IsMatch(password);
        }
 
        public List<PersonDTO> Users { get => _Users; set => Set(ref _Users, value); }

        private List<PersonDTO> _Users = new List<PersonDTO>();

        #region Пользователи
        private ICommand _GetUsersCommand;

        public ICommand GetUsersCommand => _GetUsersCommand
            ??= new LambdaCommandAsync(OnGetUsersCommandExecuted, CanGetUsersCommandExecute);

        private bool CanGetUsersCommandExecute(object p) => p is string;

        private async Task OnGetUsersCommandExecuted(object p)
        {
            if (p is string userId && !string.IsNullOrEmpty(userId))
            {
                Users = await _DataService.GetUsersAsync(userId);
                MessageBox.Show("Ресурсы успешно получены");
            }
            else
            {
                MessageBox.Show("Пользователь не авторизован");
            }
        }

        #endregion
        #region Регистрация
        private ICommand _RegistrationCommand;

        public ICommand RegistrationCommand => _RegistrationCommand
            ??= new LambdaCommandAsync(OnRegistrationCommandExecuted, CanRegistrationCommandExecute);

        private bool CanRegistrationCommandExecute() => true;

        private async Task OnRegistrationCommandExecuted()
        {
            if(!string.IsNullOrEmpty(Login)&&!string.IsNullOrEmpty(Password))
            {
                var person = new PersonDTO
                {
                    Login = Login,
                    Password = Password
                };
                MessageBox.Show(await _DataService.RegisterUserAsync(person));
            }
            else
            {
                MessageBox.Show("Поля логин и пароль должны быть заполненны");
            }
        }
        #endregion
        #region Авторизация
        private ICommand _AuthorizationCommand;

        public ICommand AuthorizationCommand => _AuthorizationCommand
            ??= new LambdaCommandAsync(OnAuthorizationCommandExecuted, CanAuthorizationCommandExecute);

        private bool CanAuthorizationCommandExecute() => true;

        private async Task OnAuthorizationCommandExecuted()
        {
            if (!string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(Password))
            {
                var person = new PersonDTO
                {
                    Login = Login,
                    Password = Password
                };
                var token_Stroke = await _DataService.AuthorizeUserAsync(person);
                
                if(token_Stroke != null)
                {
                    _TokenResponse = token_Stroke;
                    OnPropertyChanged(nameof(RefreshToken));
                    OnPropertyChanged(nameof(CurrentToken));
                }
            }
            else
            {
                MessageBox.Show("Поля логин и пароль должны быть заполненны");
            }
        }
        #endregion
        #region рефреш токен
        private ICommand _RefreshTokenCommand;

        public ICommand RefreshTokenCommand => _RefreshTokenCommand
            ??= new LambdaCommandAsync(OnRefreshTokenCommandExecuted, CanRefreshTokenCommandExecute);

        private bool CanRefreshTokenCommandExecute(object p) => p is string;

        private async Task OnRefreshTokenCommandExecuted(object p)
        {
            if (p is string userId && !string.IsNullOrEmpty(userId))
            {
                var token = await _DataService.RefreshAccessTokenAsync(userId);
                
                if(token != null)
                {
                    _TokenResponse = token;
                    OnPropertyChanged(nameof(RefreshToken));
                    OnPropertyChanged(nameof(CurrentToken));
                }
            }
            else
            {
                MessageBox.Show("Отсутствует рефреш токен");
            }
        }

        #endregion
        public MainWindowViewModel(IDataService DataService)
        {
            _DataService = DataService;
            _TokenResponse = new TokenResponse()
            {
                access_token = "",
                expires_in = 0,
                refresh_token = "",
                token_type = ""
            };
        }
    }
}
