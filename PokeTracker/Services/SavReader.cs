using PokeTracker.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace PokeTracker.Services
{
    public class SavReader
    {
        private readonly string savName;

        public SavReader(string savName)
        {
            this.savName = savName;
        }

        private readonly Dictionary<byte, char> abecedario = new Dictionary<byte, char>
        {
            { 0xBB, 'A' }, { 0xBC, 'B' }, { 0xBD, 'C' },
            { 0xBE, 'D' }, { 0xBF, 'E' }, { 0xC0, 'F' }, { 0xC1, 'G' },
            { 0xC2, 'H' }, { 0xC3, 'I' }, { 0xC4, 'J' }, { 0xC5, 'K' },
            { 0xC6, 'L' }, { 0xC7, 'M' }, { 0xC8, 'N' }, { 0xC9, 'O' },
            { 0xCA, 'P' }, { 0xCB, 'Q' }, { 0xCC, 'R' }, { 0xCD, 'S' },
            { 0xCE, 'T' }, { 0xCF, 'U' }, { 0xD0, 'V' }, { 0xD1, 'W' },
            { 0xD2, 'X' }, { 0xD3, 'Y' }, { 0xD4, 'Z' }, { 0xD5, 'a' },
            { 0xD6, 'b'},  { 0xD7, 'c' }, { 0xD8, 'd' }, { 0xD9, 'e' },
            { 0xDA, 'f'},  { 0xDB, 'g' }, { 0xDC, 'h' }, { 0xDD, 'i' },
            { 0xDE, 'j'},  { 0xDF, 'k' }, { 0xE0, 'l' }, { 0xE1, 'm' },
            { 0xE2, 'n'},  { 0xE3, 'o' }, { 0xE4, 'p' }, { 0xE5, 'q' },
            { 0xE6, 'r'},  { 0xE7, 's' }, { 0xE8, 't' }, { 0xE9, 'u' },
            { 0xEA, 'v'},  { 0xEB, 'w' }, { 0xEC, 'x' }, { 0xED, 'y' },
            { 0xEE, 'z'},  { 0xA1, '0' }, { 0xA2, '1' }, { 0xA3, '2' },
            { 0xA4, '3'},  { 0xA5, '4' }, { 0xA6, '5' }, { 0xA7, '6' },
            { 0xA8, '7'},  { 0xA9, '8' }, { 0xAA, '9' }, { 0xAB, '!' },
            { 0xAC, '?'},  { 0xB5, '♂' }, { 0xB6, '♀' }, { 0xBA, '/' },
            { 0xAE, '-'},  { 0xB0, '…' }, { 0xB1, '"' }, { 0xB2, '"' },
            { 0xB3, '\''}, { 0xB4, '\'' }, { 0xAD, '.' }, { 0xB8, ','}
        };

        private readonly Dictionary<int, string> permutations = new Dictionary<int, string>
        {
            {0, "GAEM"}, {1, "GAME"}, {2, "GEAM"}, {3, "GEMA"},
            {4, "GMAE"}, {5, "GMEA"}, {6, "AGEM"}, {7, "AGME"},
            {8, "AEGM"}, {9, "AEMG"}, {10, "AMGE"}, {11, "AMEG"},
            {12, "EGAM"}, {13, "EGMA"}, {14, "EAGM"}, {15, "EAMG"},
            {16, "EMGA"}, {17, "EMAG"}, {18, "MGAE"}, {19, "MGEA"},
            {20, "MAGE"}, {21, "MAEG"}, {22, "MEGA"}, {23, "MEAG"}
        };

        private static readonly string[] Natures =
        {
            "Hardy", "Lonely", "Brave", "Adamant", "Naughty",
            "Bold", "Docile", "Relaxed", "Impish", "Lax",
            "Timid", "Hasty", "Serious", "Jolly", "Naive",
            "Modest", "Mild", "Quiet", "Bashful", "Rash",
            "Calm", "Gentle", "Sassy", "Careful", "Quirky"
        };


        private async Task<Stream> OpenSaveAsync()
        {
            return await FileSystem.Current.OpenAppPackageFileAsync(savName);
        }

        private byte[] ReadSection(BinaryReader binaryReader, int index)
        {
            binaryReader.BaseStream.Seek(index * 4096, SeekOrigin.Begin);

            return binaryReader.ReadBytes(4096);
        }

        private byte[] GetTeamSection(BinaryReader binaryReader)
        {
            // Comparamos la posicion 0 y la 14
            byte[] firstSectionId = ReadSection(binaryReader, 0);
            byte[] secondSectionId = ReadSection(binaryReader, 14);
            uint firstSectionSaveIndex = BitConverter.ToUInt32(firstSectionId, 0xFFC);
            uint secondSectionSaveIndex = BitConverter.ToUInt32(secondSectionId, 0xFFC);

            int index = firstSectionSaveIndex > secondSectionSaveIndex ? 0 : 14;

            for (int i = index; i < (index + 14); i++)
            {
                byte[] section = ReadSection(binaryReader, i);

                ushort id = BitConverter.ToUInt16(section, 0xFF4);

                if (id == 1)
                    return section;
            }

            return null;
        }

        private byte[] GetPokemon(byte[] teamSection, int indexTeam)
        {
            byte[] pokemon = teamSection
                .Skip(0x238 + (indexTeam * 100))
                .Take(100)
                .ToArray();

            return pokemon;
        }

        public async Task<int> GetPokemonCount()
        {
            Stream stream = await OpenSaveAsync();
            BinaryReader binaryReader = new BinaryReader(stream);

            byte[] teamSection = GetTeamSection(binaryReader);

            // Get pokemon count
            int count = (int)BitConverter.ToUInt32(teamSection, 0x234);

            return count;
        }

        public async Task<int> GetPokemonCount(byte[] teamSection)
        {
            // Get pokemon count
            int count = (int)BitConverter.ToUInt32(teamSection, 0x234);

            return count;
        }

        private uint GetXorKey(byte[] pokemon)
        {
            uint personalityValue = BitConverter.ToUInt32(pokemon, 0x00);
            uint uid = BitConverter.ToUInt32(pokemon, 0x04);

            uint xorkey = personalityValue ^ uid;

            return xorkey;
        }

        private byte[] DecryptPokemonData(byte[] pokemon)
        {
            byte[] decrypted = new byte[48];
            uint xorkey = GetXorKey(pokemon);

            for (int i = 0; i < 12; i++)
            {
                uint value = BitConverter.ToUInt32(pokemon, 0x20 + (i * 4));
                value ^= xorkey;
                Array.Copy(BitConverter.GetBytes(value), 0, decrypted, i * 4, 4);
            }

            return decrypted;
        }

        private ushort GetSpeciesId(byte[] pokemon)
        {
            byte[] pokemonData = DecryptPokemonData(pokemon);
            uint pid = BitConverter.ToUInt32(pokemon, 0x00);
            uint order = pid % 24;
            string orderString = permutations[(int)order];

            // Get letter G from dictionary

            int index = 0;
            foreach (char c in orderString)
            {
                if (c == 'G')
                    break;

                index++;
            }

            ushort speciesId = BitConverter.ToUInt16(pokemonData, index * 12);

            return speciesId;
        }

        public async Task<List<Pokemon>> ReadTeam()
        {
            // Data
            Stream stream = await OpenSaveAsync();
            BinaryReader binaryReader = new BinaryReader(stream);

            // Get the team section
            byte[] teamSection = GetTeamSection(binaryReader);

            // Get Pokemon count
            int count = await GetPokemonCount(teamSection);

            // Get each pokemon in Bytes
            List<byte[]> pokemon = new List<byte[]>();

            for (int i = 0; i < count; i++)
            {
                byte[] currentPokemon = GetPokemon(teamSection, i);
                pokemon.Add(currentPokemon);
            }

            // Get each SpeciesId

            List<ushort> speciesidList = new List<ushort>();

            foreach (byte[] currentPokemon in pokemon)
                speciesidList.Add(GetSpeciesId(currentPokemon));

            // Get Information from Json and Transform in Pokemon objects

            string json = await File.ReadAllTextAsync("pokemon.json");

            List<Pokemon> pokemonData = JsonSerializer.Deserialize<List<Pokemon>>(json) ?? new();
            List<Pokemon> finalList = new List<Pokemon>();

            for (int i = 0; i < count; i++)
            {
                Pokemon? basePokemon = pokemonData.FirstOrDefault(x => x.speciesId == speciesidList[i]);

                if (basePokemon != null)
                {
                    Pokemon pkmn = new Pokemon
                    {
                        speciesId = basePokemon.speciesId,
                        numberPokedex = basePokemon.numberPokedex,
                        name = basePokemon.name,
                        type1 = basePokemon.type1,
                        type2 = basePokemon.type2,
                        ab1 = basePokemon.ab1,
                        ab2 = basePokemon.ab2,

                        nickname = GetNickname(pokemon[i]),
                        hp = GetHp(pokemon[i]),
                        maxHp = GetMaxHp(pokemon[i]),
                        atq = GetAttack(pokemon[i]),
                        spAtq = GetSpAttack(pokemon[i]),
                        def = GetDefense(pokemon[i]),
                        spDef = GetSpDefense(pokemon[i]),
                        spe = GetSpeed(pokemon[i]),

                        Level = GetLevel(pokemon[i]),
                        ImagePath = $"images/pkmn/{basePokemon.numberPokedex}.png",
                        ability = GetAbility(pokemon[i], basePokemon.ab1 ?? "", basePokemon.ab2 ?? ""),
                        nature = GetNature(pokemon[i]),

                        evHp = GetEvHp(pokemon[i]),
                        evAtq = GetEvAttack(pokemon[i]),
                        evDef = GetEvDefense(pokemon[i]),
                        evSpe = GetEvSpeed(pokemon[i]),
                        evSpAtq = GetEvSpAttack(pokemon[i]),
                        evSpDef = GetEvSpDefense(pokemon[i]),

                        ivAtq = GetIvAttack(pokemon[i]),
                        ivDef = GetIvDefense(pokemon[i]),
                        ivSpe = GetIvSpeed(pokemon[i]),
                        ivSpAtq = GetIvSpAttack(pokemon[i]),
                        ivSpDef = GetIvSpDefense(pokemon[i]),
                    };
                    finalList.Add(pkmn);
                }
            }

            return finalList;
        }

        public byte[] GetEvAndCondition(byte[] pokemon)
        {
            // Data
            byte[] pokemonData = DecryptPokemonData(pokemon);
            uint pid = BitConverter.ToUInt32(pokemon, 0x00);
            uint order = pid % 24;
            string orderString = permutations[(int)order];

            // Get letter E from dictionary

            int index = 0;
            foreach (char c in orderString)
            {
                if (c == 'E')
                    break;

                index++;
            }

            byte[] evCondition = pokemonData
                .Skip(index * 12)
                .Take(12)
                .ToArray();

            return evCondition;
        }

        private string GetNickname(byte[] pokemon)
        {
            byte[] nicknameBytes = new byte[10];
            Array.Copy(pokemon, 0x08, nicknameBytes, 0, 10);

            StringBuilder stringBuilder = new StringBuilder();

            foreach (byte b in nicknameBytes)
            {
                if (b == 0XFF)
                    break;

                if (abecedario.TryGetValue(b, out char c))
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        // -------------------- STATS ------------------------ //
        public ushort GetHp(byte[] pokemon)
        {
            return BitConverter.ToUInt16(pokemon, 0x56);
        }

        public ushort GetMaxHp(byte[] pokemon)
        {
            return BitConverter.ToUInt16(pokemon, 0x58);
        }

        public ushort GetAttack(byte[] pokemon)
        {
            return BitConverter.ToUInt16(pokemon, 0x5A);
        }

        public ushort GetSpAttack(byte[] pokemon)
        {
            return BitConverter.ToUInt16(pokemon, 0x60);
        }

        public ushort GetDefense(byte[] pokemon)
        {
            return BitConverter.ToUInt16(pokemon, 0x5C);
        }

        public ushort GetSpDefense(byte[] pokemon)
        {
            return BitConverter.ToUInt16(pokemon, 0x62);
        }

        public ushort GetSpeed(byte[] pokemon)
        {
            return BitConverter.ToUInt16(pokemon, 0x5E);
        }

        public byte GetLevel(byte[] pokemon)
        {
            return pokemon[0x54];
        }

        // ----------------- EV STATS ----------------------- //

        public byte GetEvHp(byte[] pokemon)
        {
            byte[] evCondition = GetEvAndCondition(pokemon);
            return evCondition[0];
        }

        public byte GetEvAttack(byte[] pokemon)
        {
            byte[] evCondition = GetEvAndCondition(pokemon);
            return evCondition[1];
        }

        public byte GetEvDefense(byte[] pokemon)
        {
            byte[] evCondition = GetEvAndCondition(pokemon);
            return evCondition[2];
        }

        public byte GetEvSpeed(byte[] pokemon)
        {
            byte[] evCondition = GetEvAndCondition(pokemon);
            return evCondition[3];
        }

        public byte GetEvSpAttack(byte[] pokemon)
        {
            byte[] evCondition = GetEvAndCondition(pokemon);
            return evCondition[4];
        }

        public byte GetEvSpDefense(byte[] pokemon)
        {
            byte[] evCondition = GetEvAndCondition(pokemon);
            return evCondition[5];
        }

        public byte[] GetMiscellaneous(byte[] pokemon)
        {
            // Data
            byte[] pokemonData = DecryptPokemonData(pokemon);
            uint pid = BitConverter.ToUInt32(pokemon, 0x00);
            uint order = pid % 24;
            string orderString = permutations[(int)order];

            // Get letter M from dictionary

            int index = 0;
            foreach (char c in orderString)
            {
                if (c == 'M')
                    break;

                index++;
            }

            byte[] miscellaneous = pokemonData
                .Skip(index * 12)
                .Take(12)
                .ToArray();

            return miscellaneous;
        }


        public string GetAbility(byte[] pokemon, string ab1, string ab2)
        {
            byte[] miscellaneous = GetMiscellaneous(pokemon);

            uint value = BitConverter.ToUInt32(miscellaneous, 4);

            int abilityIndex = (int)((value >> 31) & 1);

            if (abilityIndex == 0)
                return ab1;
            else
                return ab2;
        }

        public string GetNature(byte[] pokemon)
        {
            uint personalityValue = BitConverter.ToUInt32(pokemon, 0x00);
            int natureIndex = (int)(personalityValue % 25);

            return Natures[natureIndex];
        }

        // ------- IV STATS -------- //

        public byte GetIvHp(byte[] pokemon)
        {
            byte[] miscellaneous = GetMiscellaneous(pokemon);
            uint value = BitConverter.ToUInt32(miscellaneous, 4);
            return (byte)(value & 0x1F);
        }

        public byte GetIvAttack(byte[] pokemon)
        {
            byte[] miscellaneous = GetMiscellaneous(pokemon);
            uint value = BitConverter.ToUInt32(miscellaneous, 4);
            return (byte)((value >> 5) & 0x1F);
        }

        public byte GetIvDefense(byte[] pokemon)
        {
            byte[] miscellaneous = GetMiscellaneous(pokemon);
            uint value = BitConverter.ToUInt32(miscellaneous, 4);
            return (byte)((value >> 10) & 0x1F);
        }

        public byte GetIvSpeed(byte[] pokemon)
        {
            byte[] miscellaneous = GetMiscellaneous(pokemon);
            uint value = BitConverter.ToUInt32(miscellaneous, 4);
            return (byte)((value >> 15) & 0x1F);
        }

        public byte GetIvSpAttack(byte[] pokemon)
        {
            byte[] miscellaneous = GetMiscellaneous(pokemon);
            uint value = BitConverter.ToUInt32(miscellaneous, 4);
            return (byte)((value >> 20) & 0x1F);
        }

        public byte GetIvSpDefense(byte[] pokemon)
        {
            byte[] miscellaneous = GetMiscellaneous(pokemon);
            uint value = BitConverter.ToUInt32(miscellaneous, 4);
            return (byte)((value >> 25) & 0x1F);
        }
    }
}
