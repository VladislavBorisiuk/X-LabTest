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
        
        public string Login { get => _Login; set => Set(ref _Login, value); }

        private string _Login = "";

        public string Password { get => _Password; set => Set(ref _Password, value); }

        private string _Password = "";
        
        public string Users { get => _Users; set => Set(ref _Users, value); }

        private string _Users = "";

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
                MessageBox.Show(Users);
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
                }
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
