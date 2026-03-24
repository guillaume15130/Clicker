namespace MedievalClicker.Models
{
    public enum OreType
    {
        Charbon,
        Fer,
        Or,
        Diamant,
        MineraisMagique,
        CristalDePouvoir,
        MineraisAncien
    }

    public static class OreInfo
    {
        public static string GetName(OreType type) => type switch
        {
            OreType.Charbon => "Charbon",
            OreType.Fer => "Fer",
            OreType.Or => "Or",
            OreType.Diamant => "Diamant",
            OreType.MineraisMagique => "Minerais Magique",
            OreType.CristalDePouvoir => "Cristal de Pouvoir",
            OreType.MineraisAncien => "Minerais Ancien",
            _ => "Inconnu"
        };

        public static string GetEmoji(OreType type) => type switch
        {
            OreType.Charbon => "⚫",
            OreType.Fer => "⬜",
            OreType.Or => "🟡",
            OreType.Diamant => "🔷",
            OreType.MineraisMagique => "🟣",
            OreType.CristalDePouvoir => "💎",
            OreType.MineraisAncien => "🔶",
            _ => "❓"
        };

        public static double GetBaseValue(OreType type) => type switch
        {
            OreType.Charbon => 1,
            OreType.Fer => 5,
            OreType.Or => 25,
            OreType.Diamant => 100,
            OreType.MineraisMagique => 500,
            OreType.CristalDePouvoir => 2500,
            OreType.MineraisAncien => 10000,
            _ => 0
        };

        public static int GetRequiredDepth(OreType type) => type switch
        {
            OreType.Charbon => 0,
            OreType.Fer => 10,
            OreType.Or => 30,
            OreType.Diamant => 60,
            OreType.MineraisMagique => 100,
            OreType.CristalDePouvoir => 150,
            OreType.MineraisAncien => 200,
            _ => 999
        };
    }
}
