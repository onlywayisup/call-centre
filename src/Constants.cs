namespace CallCentre
{
    public static class Constants
    {
        public const string UpdateUrl = "https://raw.githubusercontent.com/femma-it/call-centre/master/version.xml";
        public const string Authority = "https://sso.qexal.app";

#if DEBUG
        public const string ApiUrl = "https://localhost:62254/";

#else
        public const string ApiUrl = "https://femma.qexal.app/module/cti/";
#endif
        public const string ClientId = "microsip";
        public const string Scope = "openid offline_access Qexal.ApiAPI";

        public const string AutoProvisioningUrl = ApiUrl + "configuration";
    }
}