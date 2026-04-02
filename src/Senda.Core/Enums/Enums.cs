namespace Senda.Core.Enums;

public enum SourceType
{
    Pdf,
    Text,
    Url
}

public enum DocumentStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum MessageRole
{
    User,
    Assistant,
    System
}
