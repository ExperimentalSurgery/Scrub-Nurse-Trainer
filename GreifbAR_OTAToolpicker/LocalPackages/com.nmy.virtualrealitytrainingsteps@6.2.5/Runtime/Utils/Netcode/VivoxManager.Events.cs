#if NMY_ENABLE_VIVOX

using VivoxUnity;

namespace NMY.VirtualRealityTraining
{
    public partial class VivoxManager
    {
        /// <summary>
        /// This delegate defines the method signature for handling the <see cref="NMY.VirtualRealityTraining.VivoxManager.OnSpeechDetectedEvent"/> event.
        /// </summary>
        public delegate void ParticipantValueChangedHandler(string username, ChannelId channel, bool value);

        /// <summary>
        /// An event that is raised when the speech of a participant is detected.
        /// </summary>
        public static event ParticipantValueChangedHandler OnSpeechDetectedEvent;

        /// <summary>
        /// This delegate defines the method signature for handling the <see cref="NMY.VirtualRealityTraining.VivoxManager.OnAudioEnergyChangedEvent"/> event.
        /// </summary>
        public delegate void ParticipantValueUpdatedHandler(string username, ChannelId channel, double value);

        /// <summary>
        /// An event that is raised when the audio energy of a participant changes.
        /// </summary>
        public static event ParticipantValueUpdatedHandler OnAudioEnergyChangedEvent;

        /// <summary>
        /// This delegate defines the method signature for handling the <see cref="NMY.VirtualRealityTraining.VivoxManager.OnParticipantAddedEvent"/> and
        /// <see cref="NMY.VirtualRealityTraining.VivoxManager.OnParticipantRemovedEvent"/> events.
        /// </summary>
        public delegate void ParticipantStatusChangedHandler(string username, ChannelId channel,
                                                             IParticipant participant);

        /// <summary>
        /// An event that is raised when a participant is added to a channel.
        /// </summary>
        public static event ParticipantStatusChangedHandler OnParticipantAddedEvent;

        /// <summary>
        /// An event that is raised when a participant is removed to a channel.
        /// </summary>
        public static event ParticipantStatusChangedHandler OnParticipantRemovedEvent;

        /// <summary>
        /// This delegate defines the method signature for handling the <see cref="NMY.VirtualRealityTraining.VivoxManager.OnTextMessageLogReceivedEvent"/> event.
        /// </summary>
        public delegate void ChannelTextMessageChangedHandler(string sender, IChannelTextMessage channelTextMessage);

        /// <summary>
        /// An event that is raised when a text message is received in a channel.
        /// </summary>
        public static event ChannelTextMessageChangedHandler OnTextMessageLogReceivedEvent;

        /// <summary>
        /// This delegate defines the method signature for handling the <see cref="NMY.VirtualRealityTraining.VivoxManager.OnUserLoggedInEvent"/> and
        /// <see cref="NMY.VirtualRealityTraining.VivoxManager.OnUserLoggedOutEvent"/> events.
        /// </summary>
        public delegate void LoginStatusChangedHandler();

        /// <summary>
        /// An event that is raised when a user logs in.
        /// </summary>
        public static event LoginStatusChangedHandler OnUserLoggedInEvent;

        /// <summary>
        /// An event that is raised when a user logs out.
        /// </summary>
        public static event LoginStatusChangedHandler OnUserLoggedOutEvent;
    }
}

#endif // NMY_ENABLE_VIVOX
