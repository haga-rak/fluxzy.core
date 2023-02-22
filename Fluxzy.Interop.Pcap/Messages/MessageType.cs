namespace Fluxzy.Capturing.Messages;

public enum MessageType : byte
{
    Exit = 0,
    Subscribe = 1 , 
    Unsubscribe = 2,
    Include = 3,
    Ready = 100
}