namespace CallCentre
{
    public static class Constants
    {
        public const string UpdateUrl = "https://raw.githubusercontent.com/femma-it/call-centre/master/version.xml";
        public const string ApiUrl = "https://femma.qexal.xyz";
        public const string AutoProvisioningUrl = ApiUrl + "/api/v1/microsip/configuration";
        public const string ClientId = "microsip";
        public const string Scope = "openid profile offline_access Qexal.ApiAPI";
    }
}