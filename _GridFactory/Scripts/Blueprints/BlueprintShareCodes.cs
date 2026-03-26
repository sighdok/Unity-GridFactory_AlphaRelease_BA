namespace GridFactory.Blueprints
{
    /*
        public static class BlueprintShareCodec
        {

            private const int CURRENT_VERSION = 1;
            private const string PREFIX_JSON = "GF_BP_JSON:";
            private const string PREFIX_GZIP = "GF_BP_GZIP:";

            // -------- Recipe Cache (Resources) --------
            private static Dictionary<string, RecipeDefinition> _recipeCache;
            private static bool _recipeCacheBuilt;

            private static RecipeDefinition ResolveRecipeById(string recipeId)
            {
                if (string.IsNullOrEmpty(recipeId))
                    return null;

                EnsureRecipeCache();

                if (_recipeCache.TryGetValue(recipeId, out var r) && r != null)
                    return r;

                return null;
            }

            private static void EnsureRecipeCache()
            {
                if (_recipeCacheBuilt) return;

                _recipeCache = new Dictionary<string, RecipeDefinition>(StringComparer.Ordinal);

                var allRecipes = Resources.LoadAll<RecipeDefinition>("");

                foreach (var r in allRecipes)
                {
                    if (r == null || string.IsNullOrEmpty(r.id)) continue;
                    _recipeCache[r.id] = r;
                }

                _recipeCacheBuilt = true;
            }

            public static void RebuildRecipeCache()
            {
                _recipeCacheBuilt = false;
                _recipeCache = null;
                EnsureRecipeCache();
            }

            public static string ExportToText(BlueprintDefinition bp, bool compress = true)
            {
                if (bp == null) throw new ArgumentNullException(nameof(bp));

                var dto = BlueprintDTO.FromBlueprint(bp, CURRENT_VERSION);
                string json = JsonUtility.ToJson(dto, prettyPrint: false);

                if (!compress)
                    return PREFIX_JSON + json;

                byte[] gz = Gzip(Encoding.UTF8.GetBytes(json));
                string b64 = Convert.ToBase64String(gz);
                return PREFIX_GZIP + b64;
            }

            public static BlueprintDefinition ImportFromText(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    throw new ArgumentException("Text ist leer.", nameof(text));

                string json = DecodeToJson(text);
                var dto = JsonUtility.FromJson<BlueprintDTO>(json);

                if (dto == null)
                    throw new Exception("Import fehlgeschlagen: DTO == null (JSON ungültig?).");

                if (dto.version <= 0)
                    throw new Exception("Import fehlgeschlagen: version fehlt/ungültig.");

                return dto.ToBlueprint(ResolveRecipeById);
            }

            // ---------- INTERNALS ----------

            private static string DecodeToJson(string text)
            {
                text = text.Trim();

                if (text.StartsWith(PREFIX_JSON, StringComparison.Ordinal))
                    return text.Substring(PREFIX_JSON.Length);

                if (text.StartsWith(PREFIX_GZIP, StringComparison.Ordinal))
                {
                    string b64 = text.Substring(PREFIX_GZIP.Length);
                    byte[] gz = Convert.FromBase64String(b64);
                    byte[] raw = Gunzip(gz);
                    return Encoding.UTF8.GetString(raw);
                }

                if (text.StartsWith("{") && text.EndsWith("}"))
                    return text;

                throw new Exception("Unbekanntes Blueprint-Format. Erwartet GF_BP_JSON:, GF_BP_GZIP: oder raw JSON.");
            }

            private static byte[] Gzip(byte[] input)
            {
                using var ms = new MemoryStream();

                // Kompatibel mit älteren Unity/.NET Profilen:
                using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
                {
                    gz.Write(input, 0, input.Length);
                }

                return ms.ToArray();
            }

            private static byte[] Gunzip(byte[] input)
            {
                using var src = new MemoryStream(input);
                using var gz = new GZipStream(src, CompressionMode.Decompress);
                using var dst = new MemoryStream();
                gz.CopyTo(dst);
                return dst.ToArray();
            }
        }

        // ---------------- DTOs ----------------

        [Serializable]
        internal class BlueprintDTO
        {
            public int version;

            public string id;
            public string displayName;

            public int machineCount;
            public int beltCount;
            public int blueprintCells;

            public Vec2i size;

            public bool hasInputPort;
            public List<PortDTO> inputPorts;

            public bool hasOutputPort;
            public PortDTO outputPort;

            public float ticksPerProcess;

            public List<ElementDTO> elements;

            public static BlueprintDTO FromBlueprint(BlueprintDefinition bp, int version)
            {
                var dto = new BlueprintDTO
                {
                    version = version,
                    id = bp.id,
                    displayName = bp.displayName,
                    machineCount = bp.machineCount,
                    beltCount = bp.beltCount,
                    blueprintCells = bp.blueprintCells,
                    size = new Vec2i(bp.size),
                    hasInputPort = bp.hasInputPort,
                    hasOutputPort = bp.hasOutputPort,
                    ticksPerProcess = bp.ticksPerProcess,
                    inputPorts = new List<PortDTO>(),
                    elements = new List<ElementDTO>()
                };

                if (bp.inputPorts != null)
                {
                    foreach (var p in bp.inputPorts)
                        dto.inputPorts.Add(PortDTO.FromPort(p));
                }

                if (bp.hasOutputPort)
                    dto.outputPort = PortDTO.FromPort(bp.outputPort);

                if (bp.elements != null)
                {
                    foreach (var e in bp.elements)
                        dto.elements.Add(ElementDTO.FromElement(e));
                }

                return dto;
            }

            public BlueprintDefinition ToBlueprint(Func<string, RecipeDefinition> recipeResolver)
            {
                var bp = ScriptableObject.CreateInstance<BlueprintDefinition>();

                bp.id = id;
                bp.displayName = displayName;

                bp.machineCount = machineCount;
                bp.beltCount = beltCount;
                bp.blueprintCells = blueprintCells;

                bp.size = size.ToVector2Int();

                bp.hasInputPort = hasInputPort;
                bp.inputPorts = new List<BlueprintPort>();

                if (inputPorts != null)
                {
                    foreach (var p in inputPorts)
                        bp.inputPorts.Add(p.ToPort());
                }

                bp.hasOutputPort = hasOutputPort;
                if (hasOutputPort && outputPort != null)
                    bp.outputPort = outputPort.ToPort();

                bp.ticksPerProcess = ticksPerProcess;

                bp.elements = new List<BlueprintElementData>();
                if (elements != null)
                {
                    foreach (var e in elements)
                        bp.elements.Add(e.ToElement(recipeResolver));
                }

                return bp;
            }
        }

        [Serializable]
        internal class ElementDTO
        {
            public BlueprintElementType elementType;
            public MachineKind machineKind;
            public string recipeId;
            public PortKind portKind;
            public Vec2i localPos;
            public Direction inputDirection;
            public Direction outputDirection;

            public static ElementDTO FromElement(BlueprintElementData e)
            {
                return new ElementDTO
                {
                    elementType = e.elementType,
                    machineKind = e.machineKind,
                    recipeId = e.currentRecipe != null ? e.currentRecipe.id : null,
                    portKind = e.portKind,
                    localPos = new Vec2i(e.localPos),
                    inputDirection = e.inputDirection,
                    outputDirection = e.outputDirection
                };
            }

            public BlueprintElementData ToElement(Func<string, RecipeDefinition> recipeResolver)
            {
                var e = new BlueprintElementData
                {
                    elementType = elementType,
                    machineKind = machineKind,
                    portKind = portKind,
                    localPos = localPos.ToVector2Int(),
                    inputDirection = inputDirection,
                    outputDirection = outputDirection,
                    currentRecipe = null
                };

                if (!string.IsNullOrEmpty(recipeId) && recipeResolver != null)
                    e.currentRecipe = recipeResolver(recipeId);

                return e;
            }

        }

        [Serializable]
        internal class PortDTO
        {
            public Vec2i localPos;
            public Direction facing;
            public ItemType itemType;

            public static PortDTO FromPort(BlueprintPort p)
            {
                return new PortDTO
                {
                    localPos = new Vec2i(p.localPos),
                    facing = p.facing,
                    itemType = p.itemType
                };
            }

            public BlueprintPort ToPort()
            {
                return new BlueprintPort
                {
                    localPos = localPos.ToVector2Int(),
                    facing = facing,
                    itemType = itemType,
                    machineRef = null // nicht serialisierbar / nicht sharebar
                };
            }
        }

        [Serializable]
        internal struct Vec2i
        {
            public int x;
            public int y;

            public Vec2i(Vector2Int v) { x = v.x; y = v.y; }
            public Vector2Int ToVector2Int() => new Vector2Int(x, y);
        }
        */
}
