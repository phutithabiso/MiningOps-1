using MiningOps.Models.Entities;
using System.Diagnostics;

namespace MiningOps.Services
{
    public interface IAuthenticationService
    {
        Task<bool> LoginAsync(string username, string password);
        void Logout();
        RegisterMining? CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(UserRole role);
        event EventHandler? AuthenticationStateChanged;
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserService _userService;
        private RegisterMining? _currentUser;

        public AuthenticationService(UserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            Debug.WriteLine("🔐 AuthenticationService initialized");
        }

        public RegisterMining? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public bool IsInRole(UserRole role)
        {
            return _currentUser?.Role == role;
        }

        public event EventHandler? AuthenticationStateChanged;

        public async Task<bool> LoginAsync(string usernameOrEmail, string password)
        {
            try
            {
                Debug.WriteLine($"🔐 AuthenticationService: Attempting login for {usernameOrEmail}");

                // Use the updated UserService validation that accepts username or email
                var isValid = await _userService.ValidateUserAsync(usernameOrEmail, password);
                Debug.WriteLine($"🔐 AuthenticationService: User validation result: {isValid}");

                if (isValid)
                {
                    var user = await _userService.GetUserByUsernameOrEmailAsync(usernameOrEmail);
                    Debug.WriteLine($"🔐 AuthenticationService: User retrieved - {(user != null ? "Success" : "Null")}");

                    if (user != null)
                    {
                        _currentUser = user;
                        Debug.WriteLine($"🔐 AuthenticationService: User authenticated - {user.Username}, Email: {user.Email}, Role: {user.Role}, AccId: {user.AccId}");

                        AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
                        return true;
                    }
                }

                Debug.WriteLine($"🔐 AuthenticationService: Login failed for {usernameOrEmail}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 AuthenticationService error: {ex.Message}");
                return false;
            }
        }

        public void Logout()
        {
            Debug.WriteLine("🔐 AuthenticationService: Logging out");
            _currentUser = null;
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}