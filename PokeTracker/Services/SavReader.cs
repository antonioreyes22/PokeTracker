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
            { 0xD2, 'X' }, { 0xD3, 'Y' }, { 0xD4, 'Z' }
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
            int count = (int) BitConverter.ToUInt32(teamSection, 0x234);

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

            for(int i=0; i<count; i++) 
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

            for (int i=0; i<count; i++) 
            {
                // Find equal SpeciesId in Json
                Pokemon? pkmn = pokemonData.FirstOrDefault(x => x.speciesId == speciesidList[i]);

                if(pkmn != null)
                    finalList.Add(pkmn);
            }

            return finalList;
        }
    }
}
