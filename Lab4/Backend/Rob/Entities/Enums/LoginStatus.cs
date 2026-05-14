public enum LoginStatus
{
    Success = 200,
    IncorrectEmail,
    IncorrectPassword,
    UnregisteredGoogle,
    UnknownOathProvider = 403,
    InvalidToken
}
