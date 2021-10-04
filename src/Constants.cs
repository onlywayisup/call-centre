namespace CallCentre
{
    public static class Constants
    {
        public const string UpdateUrl = "https://raw.githubusercontent.com/femma-it/call-centre/master/version.xml";
        //public const string ApiUrl = "https://femma.qexal.xyz";
        public const string ApiUrl = "https://localhost:5011";
        public const string AutoProvisioningUrl = ApiUrl + "/api/v1/microsip/configuration";
        public const string ClientId = "microsip";
        public const string ClientSecret = "cf6a422b-f6f1-498d-b805-77ea81234bc8";
        public const string Scope = "openid profile offline_access Qexal.ApiAPI";
        public const string RedirectUri = "http://localhost/winforms.client";
    }
}