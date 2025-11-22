namespace NavalCommand.Data
{
    public static class WeaponConstants
    {
        public static class FlagshipGun
        {
            public const float Range = 150000f;
            public const float Cooldown = 3f;
            public const float Damage = 30f;
            public const float ProjectileSpeed = 762f;
            public const float RotationSpeed = 30f;
            public const float Spread = 0.1f;
        }

        public static class Missile
        {
            public const float Range = 120000f;
            public const float Cooldown = 10f;
            public const float Damage = 60f;
            public const float ProjectileSpeed = 290f;
            public const float RotationSpeed = 45f;
            public const float Spread = 0f;
        }

        public static class Torpedo
        {
            public const float Range = 10000f;
            public const float Cooldown = 12f;
            public const float Damage = 100f;
            public const float ProjectileSpeed = 28f;
            public const float RotationSpeed = 30f;
            public const float Spread = 0f;
        }

        public static class Autocannon
        {
            public const float Range = 2500f;
            public const float Cooldown = 0.2f;
            public const float Damage = 5f;
            public const float ProjectileSpeed = 1100f;
            public const float RotationSpeed = 120f;
            public const float Spread = 0.8f;
        }

        public static class CIWS
        {
            public const float Range = 1500f;
            public const float Cooldown = 0.004f; // 15000 RPM
            public const float Damage = 2f;
            public const float ProjectileSpeed = 1100f;
            public const float RotationSpeed = 115f; // Real Phalanx speed
            public const float Spread = 0.3f;
        }
    }
}
