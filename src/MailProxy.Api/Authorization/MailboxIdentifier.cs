namespace MailProxy.Api.Authorization
{
    public struct MailboxIdentifier
    {
        public MailboxIdentifier(string mailAddress)
        {
            Mail = mailAddress;
        }

        /// <summary>
        /// The azure id for the ad group.
        /// </summary>
        public string Mail { get; set; }
    }
}
