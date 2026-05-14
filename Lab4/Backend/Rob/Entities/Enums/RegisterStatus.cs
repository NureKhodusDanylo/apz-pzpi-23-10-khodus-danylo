public enum RegisterStatus
{
    Success = 200,
    EmailBusy,
    UnknownOathProvider = 403,
    InvalidToken = 401,
    InvalidAdminKey = 400,
}