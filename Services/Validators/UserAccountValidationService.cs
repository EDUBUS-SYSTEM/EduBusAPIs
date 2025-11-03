using Data.Repos.Interfaces;

namespace Services.Validators
{
    public class UserAccountValidationService
    {
        private readonly IUserAccountRepository _userAccountRepository;

        public UserAccountValidationService(IUserAccountRepository userAccountRepository)
        {
            _userAccountRepository = userAccountRepository;
        }

        /// <summary>
        /// Validates that an email is unique (not already in use)
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <exception cref="InvalidOperationException">Thrown if email already exists</exception>
        public async Task ValidateUniqueEmailAsync(string email)
        {
            if (await _userAccountRepository.IsEmailExistAsync(email))
            {
                throw new InvalidOperationException("Email already exists.");
            }
        }

        /// <summary>
        /// Validates that a phone number is unique (not already in use)
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <exception cref="InvalidOperationException">Thrown if phone number already exists</exception>
        public async Task ValidateUniquePhoneAsync(string phoneNumber)
        {
            if (await _userAccountRepository.IsPhoneNumberExistAsync(phoneNumber))
            {
                throw new InvalidOperationException("Phone number already exists.");
            }
        }

        /// <summary>
        /// Validates that both email and phone number are unique
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <exception cref="InvalidOperationException">Thrown if either email or phone number already exists</exception>
        public async Task ValidateEmailAndPhoneAsync(string email, string phoneNumber)
        {
            await ValidateUniqueEmailAsync(email);
            await ValidateUniquePhoneAsync(phoneNumber);
        }
    }
}
