using ServiceStack;

namespace SkillChat.Server
{
    public static class ServiceStackHelper
    {
        static ServiceStackHelper()
        {
            string licenseKeyText = @"TRIAL30WEB-e1JlZjpUUklBTDMwV0VCLE5hbWU6Mi8xMC8yMDIxIGYxODQ5NzdjZjAyNTRlMmI5MmNhNjBlYjdiYzdlNWNhLFR5cGU6VHJpYWwsTWV0YTowLEhhc2g6UWg0dENmbDJENWdCaG0xNDRyYjAvUzNpZERmOHY4MFpnbVV0US8xNVBkdmJCbUtXcVU1MzU1cFFwc2lsbVNwcWJQT1pkWGRPdStEeGE2aGtveDJFdm1oL3V2eFU0M2pVeHhWYUJyS05IdGl0bklVd09NbGt5bHVGVnNWRXdVb3lzb0ZkMzFCN3Jjb3Q3WWxkUm5qKzc5b1ZSazd4UXN3VndWVDgzbEFlTlFRPSxFeHBpcnk6MjAyMS0wMy0xMn0=";
            Licensing.RegisterLicense(licenseKeyText);
        }

        public static void Help() { }
    }
}