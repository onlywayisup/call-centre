namespace CallCentre
{
    public static class Constants
    {
        public const string UpdateUrl = "https://raw.githubusercontent.com/femma-it/call-centre/master/version.xml";
#if DEBUG
        public const string ApiUrl = "https://localhost:62254/";
        public const string Authority = "https://localhost:5011";

#else
        public const string ApiUrl = "https://femma.qexal.app/module/cti/";
        public const string Authority = "https://femma.qexal.app";
#endif
        public const string ClientId = "microsip";
        public const string Scope = "openid profile offline_access Qexal.ApiAPI";

        public const string AutoProvisioningUrl = ApiUrl + "configuration";
    }
}