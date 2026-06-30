using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace Tron4Biz.Crypto;

public class Mnemonic
{
    private readonly byte[] _entropy;
    private readonly string[] _words;

    public string[] Words => _words;
    public string Sentence => string.Join(" ", _words);
    public byte[] Entropy => _entropy.ToArray();

    public Mnemonic(string[] words)
    {
        if (words == null || words.Length == 0)
            throw new ArgumentException("Words cannot be null or empty", nameof(words));

        if (!IsValidWordCount(words.Length))
            throw new ArgumentException($"Word count must be 12, 15, 18, 21, or 24, got {words.Length}");

        _words = words.Select(w => w.ToLowerInvariant().Trim()).ToArray();
        _entropy = Array.Empty<byte>();
    }

    public Mnemonic(byte[] entropy)
    {
        if (entropy == null || entropy.Length < 16 || entropy.Length > 32)
            throw new ArgumentException("Entropy must be between 16 and 32 bytes", nameof(entropy));

        if (entropy.Length % 4 != 0)
            throw new ArgumentException("Entropy length must be multiple of 4", nameof(entropy));

        _entropy = entropy.ToArray();
        _words = GenerateMnemonicWords(entropy);
    }

    public static Mnemonic Generate(int wordCount = 12)
    {
        if (!IsValidWordCount(wordCount))
            throw new ArgumentException($"Word count must be 12, 15, 18, 21, or 24, got {wordCount}");

        int entropySize = wordCount switch
        {
            12 => 16,
            15 => 20,
            18 => 24,
            21 => 28,
            24 => 32,
            _ => throw new ArgumentException("Invalid word count")
        };

        byte[] entropy = new byte[entropySize];
        RandomNumberGenerator.Fill(entropy);
        return new Mnemonic(entropy);
    }

    public static Mnemonic FromSentence(string sentence)
    {
        var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new Mnemonic(words);
    }

    public byte[] ToSeed(string passphrase = "")
    {
        string salt = "mnemonic" + passphrase;
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt);

        return Pbkdf2(_entropy, saltBytes, 2048, 64);
    }

    public bool ValidateChecksum()
    {
        if (_entropy.Length == 0)
            throw new InvalidOperationException("Entropy not available. Create Mnemonic from entropy.");

        var expectedWords = GenerateMnemonicWords(_entropy);
        return _words.SequenceEqual(expectedWords);
    }

    private static bool IsValidWordCount(int count)
    {
        return count is 12 or 15 or 18 or 21 or 24;
    }

    private static byte[] Pbkdf2(byte[] password, byte[] salt, int iterations, int outputLength)
    {
        using var hmac = new HMACSHA512(password);
        byte[] buffer = new byte[salt.Length + 4];
        Array.Copy(salt, 0, buffer, 0, salt.Length);

        byte[] result = new byte[outputLength];
        byte[] u = new byte[hmac.HashSize / 8];
        byte[] t = new byte[outputLength];
        int offset = 0;

        for (int i = 1; offset < outputLength; i++)
        {
            BitConverter.GetBytes(i).CopyTo(buffer, salt.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer, salt.Length, 4);

            u = hmac.ComputeHash(buffer);
            Array.Copy(u, 0, t, 0, u.Length);

            for (int j = 1; j < iterations; j++)
            {
                u = hmac.ComputeHash(u);
                for (int k = 0; k < t.Length; k++)
                    t[k] ^= u[k];
            }

            Array.Copy(t, 0, result, offset, Math.Min(t.Length, outputLength - offset));
            offset += t.Length;
        }

        return result;
    }

    private static string[] GenerateMnemonicWords(byte[] entropy)
    {
        int totalBits = entropy.Length * 8;
        int checksumBits = totalBits / 32;
        int entropyBits = totalBits - checksumBits;

        byte[] checksum = SHA256.HashData(entropy);
        int checksumByte = checksum[0] >> (8 - checksumBits);

        BitArray bits = new BitArray(entropy);
        BitArray checkBits = new BitArray(new[] { (byte)checksumByte });
        BitArray allBits = new BitArray(totalBits + checksumBits);

        for (int i = 0; i < entropyBits; i++)
            allBits[i] = bits[i];
        for (int i = 0; i < checksumBits; i++)
            allBits[entropyBits + i] = checkBits[i];

        string[] wordlist = Bip39English.WordList;
        int wordCount = allBits.Length / 11;
        string[] words = new string[wordCount];

        for (int i = 0; i < wordCount; i++)
        {
            int index = 0;
            for (int j = 0; j < 11; j++)
            {
                index <<= 1;
                if (allBits[i * 11 + j])
                    index |= 1;
            }
            words[i] = wordlist[index];
        }

        return words;
    }
}

public static class Bip39English
{
    private static readonly string[] _wordList = new[]
    {
        "abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract", "absurd", "abuse",
        "access", "accident", "account", "accuse", "achieve", "acid", "acoustic", "acquire", "across", "act",
        "action", "actor", "actress", "actual", "adapt", "add", "addict", "address", "adjust", "admit",
        "adult", "advance", "advice", "aerobic", "affair", "afford", "afraid", "again", "age", "agent",
        "agree", "ahead", "aim", "air", "airport", "aisle", "alarm", "album", "alcohol", "alert",
        "alien", "all", "alley", "allow", "almost", "alone", "alpha", "already", "also", "alter",
        "always", "amateur", "amazing", "among", "amount", "amused", "analyst", "anchor", "ancient", "anger",
        "angle", "angry", "animal", "ankle", "announce", "annual", "another", "answer", "antenna", "antique",
        "anxiety", "any", "apart", "apology", "appear", "apple", "approve", "april", "arch", "arctic",
        "area", "arena", "argue", "arm", "armed", "armor", "army", "around", "arrange", "arrest",
        "arrive", "arrow", "art", "artefact", "artist", "artwork", "ask", "aspect", "assault", "asset",
        "assist", "assume", "asthma", "athlete", "atom", "attack", "attend", "attitude", "attract", "auction",
        "audit", "august", "aunt", "author", "auto", "autumn", "average", "avocado", "avoid", "awake",
        "aware", "away", "awesome", "awful", "awkward", "axis", "baby", "bachelor", "bacon", "badge",
        "bag", "balance", "balcony", "ball", "bamboo", "banana", "banner", "bar", "barely", "bargain",
        "barrel", "base", "basic", "basket", "battle", "beach", "bean", "beauty", "because", "become",
        "beef", "before", "begin", "behave", "behind", "believe", "below", "belt", "bench", "benefit",
        "best", "betray", "better", "between", "beyond", "bicycle", "bid", "bike", "bind", "biology",
        "bird", "birth", "bitter", "black", "blade", "blame", "blanket", "blast", "bleak", "bless",
        "blind", "blood", "blossom", "blouse", "blue", "blur", "blush", "board", "boat", "body",
        "boil", "bomb", "bone", "bonus", "book", "boost", "border", "boring", "borrow", "boss",
        "bottom", "bounce", "box", "boy", "bracket", "brain", "brand", "brass", "brave", "bread",
        "breeze", "brick", "bridge", "brief", "bright", "bring", "brisk", "broccoli", "broken", "bronze",
        "broom", "brother", "brown", "brush", "bubble", "buddy", "budget", "buffalo", "build", "bulb",
        "bulk", "bullet", "bundle", "bunker", "burden", "burger", "burst", "bus", "business", "busy",
        "butter", "buyer", "buzz", "cabbage", "cabin", "cable", "cactus", "cage", "cake", "call",
        "calm", "camera", "camp", "can", "canal", "cancel", "candy", "cannon", "canoe", "canvas",
        "canyon", "capable", "capital", "captain", "car", "carbon", "card", "cargo", "carpet", "carry",
        "cart", "case", "cash", "casino", "castle", "casual", "cat", "catalog", "catch", "category",
        "cattle", "caught", "cause", "caution", "cave", "ceiling", "celery", "cement", "census", "century",
        "cereal", "certain", "chair", "chalk", "champion", "change", "chaos", "chapter", "charge", "chase",
        "chat", "cheap", "check", "cheese", "chef", "cherry", "chest", "chicken", "chief", "child",
        "chimney", "choice", "choose", "chronic", "chuckle", "chunk", "churn", "cigar", "cinnamon", "circle",
        "citizen", "city", "civil", "claim", "clap", "clarify", "claw", "clay", "clean", "clerk",
        "clever", "click", "client", "cliff", "climb", "clinic", "clip", "clock", "clog", "close",
        "cloth", "cloud", "clown", "club", "clump", "cluster", "clutch", "coach", "coast", "coconut",
        "code", "coffee", "coil", "coin", "collect", "color", "column", "combine", "come", "comfort",
        "comic", "common", "company", "concert", "conduct", "confirm", "congress", "connect", "consider", "control",
        "convince", "cook", "cool", "copper", "copy", "coral", "core", "corn", "correct", "cost",
        "cotton", "couch", "country", "couple", "course", "cousin", "cover", "coyote", "crack", "cradle",
        "craft", "cram", "crane", "crash", "crater", "crawl", "crazy", "cream", "credit", "creek",
        "crew", "cricket", "crime", "crisp", "critic", "crop", "cross", "crouch", "crowd", "crucial",
        "cruel", "cruise", "crumble", "crunch", "crush", "cry", "crystal", "cube", "culture", "cup",
        "cupboard", "curious", "current", "curtain", "curve", "cushion", "custom", "cute", "cycle", "dad",
        "damage", "damp", "dance", "danger", "daring", "dash", "daughter", "dawn", "day", "deal",
        "debate", "debris", "decade", "december", "decide", "decline", "decorate", "decrease", "deer", "defense",
        "define", "defy", "degree", "delay", "deliver", "demand", "demise", "denial", "dentist", "deny",
        "depart", "depend", "deposit", "depth", "deputy", "derive", "describe", "desert", "design", "desk",
        "despair", "destroy", "detail", "detect", "develop", "device", "devote", "diagram", "dial", "diamond",
        "diary", "dice", "diesel", "diet", "differ", "digital", "dignity", "dilemma", "dinner", "dinosaur",
        "direct", "dirt", "disagree", "discover", "disease", "dish", "dismiss", "disorder", "display", "distance",
        "divert", "divide", "divorce", "dizzy", "doctor", "document", "dog", "doll", "dolphin", "domain",
        "donate", "donkey", "donor", "door", "dose", "double", "dove", "draft", "dragon", "drama",
        "drastic", "draw", "dream", "dress", "drift", "drill", "drink", "drip", "drive", "drop",
        "drum", "dry", "duck", "dumb", "dune", "during", "dust", "dutch", "duty", "dwarf",
        "dynamic", "eager", "eagle", "early", "earn", "earth", "easily", "east", "easy", "echo",
        "ecology", "economy", "edge", "edit", "educate", "effort", "egg", "eight", "either", "elbow",
        "elder", "electric", "elegant", "element", "elephant", "elevator", "elite", "else", "embark", "embody",
        "embrace", "emerge", "emotion", "employ", "empower", "empty", "enable", "enact", "end", "endless",
        "endorse", "enemy", "energy", "enforce", "engage", "engine", "enhance", "enjoy", "enlist", "enough",
        "enrich", "enroll", "ensure", "enter", "entire", "entry", "envelope", "episode", "equal", "equip",
        "era", "erase", "erode", "erosion", "error", "erupt", "escape", "essay", "essence", "estate",
        "eternal", "ethics", "evidence", "evil", "evoke", "evolve", "exact", "example", "excess", "exchange",
        "excite", "exclude", "excuse", "execute", "exercise", "exhaust", "exhibit", "exile", "exist", "exit",
        "exotic", "expand", "expect", "expire", "explain", "expose", "express", "extend", "extra", "eye",
        "eyebrow", "fabric", "face", "faculty", "fade", "faint", "faith", "fall", "false", "fame",
        "family", "famous", "fan", "fancy", "fantasy", "farm", "fashion", "fat", "fatal", "father",
        "fatigue", "fault", "favorite", "feature", "february", "federal", "fee", "feed", "feel", "female",
        "fence", "festival", "fetch", "fever", "few", "fiber", "fiction", "field", "figure", "file",
        "film", "filter", "final", "find", "fine", "finger", "finish", "fire", "firm", "first",
        "fiscal", "fish", "fit", "fitness", "fix", "flag", "flame", "flash", "flat", "flavor",
        "flee", "flight", "flip", "float", "flock", "floor", "flower", "fluid", "flush", "fly",
        "foam", "focus", "fog", "foil", "fold", "follow", "food", "foot", "force", "forest",
        "forget", "fork", "fortune", "forum", "forward", "fossil", "foster", "found", "fox", "fragile",
        "frame", "frequent", "fresh", "friend", "fringe", "frog", "front", "frost", "frown", "frozen",
        "fruit", "fuel", "fun", "funny", "furnace", "fury", "future", "gadget", "gain", "galaxy",
        "gallery", "game", "gap", "garage", "garbage", "garden", "garlic", "garment", "gas", "gasp",
        "gate", "gather", "gauge", "gaze", "general", "genius", "genre", "gentle", "genuine", "gesture",
        "ghost", "giant", "gift", "giggle", "ginger", "giraffe", "girl", "give", "glad", "glance",
        "glare", "glass", "gleam", "globe", "gloom", "glory", "glove", "glow", "glue", "goal",
        "goat", "goes", "gold", "golf", "good", "goose", "govern", "gown", "grab", "grace",
        "grade", "grain", "grand", "grant", "grape", "graph", "grasp", "grass", "grate", "grave",
        "gravy", "gray", "grease", "great", "green", "greet", "grief", "grill", "grin", "grind",
        "grip", "grocery", "groan", "groom", "gross", "group", "grow", "grumble", "grunt", "guard",
        "guess", "guest", "guide", "guilt", "guitar", "gun", "gust", "gym", "habit", "hair",
        "half", "hammer", "hamster", "hand", "happy", "harbor", "hard", "harsh", "harvest", "haste",
        "hat", "hatch", "haunt", "have", "hawk", "hazard", "head", "health", "heart", "heavy",
        "hedgehog", "height", "hello", "helmet", "help", "hen", "hero", "hidden", "high", "hill",
        "hint", "hip", "hire", "history", "hobby", "hockey", "hold", "hole", "holiday", "hollow",
        "home", "honey", "honor", "hope", "horn", "horror", "horse", "hospital", "host", "hotel",
        "hour", "hover", "hub", "huge", "human", "humble", "humor", "hundred", "hungry", "hunt",
        "hurdle", "hurry", "hurt", "husband", "hybrid", "ice", "icon", "idea", "ideal", "identity",
        "idle", "ignore", "ill", "illegal", "illness", "image", "imitate", "immense", "immune", "impact",
        "impose", "improve", "impulse", "inch", "include", "income", "increase", "index", "indicate", "indoor",
        "industry", "infant", "inflict", "inform", "inhale", "inherit", "initial", "injure", "ink", "innate",
        "inner", "innocent", "input", "inquiry", "insect", "inside", "insight", "inspire", "install", "intact",
        "intake", "intend", "intense", "interact", "interest", "interior", "internal", "interval", "into", "invest",
        "invite", "involve", "iron", "island", "isolate", "issue", "item", "ivory", "jacket", "jaguar",
        "jar", "jazz", "jealous", "jeans", "jelly", "jewel", "job", "join", "joke", "journey", "joy",
        "judge", "juice", "jump", "jungle", "junior", "junk", "just", "kangaroo", "keen", "keep",
        "ketchup", "key", "kick", "kid", "kidney", "kind", "king", "kiss", "kit", "kitchen", "kite",
        "kitten", "kiwi", "knee", "knife", "knight", "knit", "knock", "knot", "know", "knowledge", "lab",
        "label", "labor", "ladder", "lake", "lamb", "lamp", "land", "landscape", "lane", "language",
        "laptop", "large", "later", "latin", "laugh", "laundry", "lava", "law", "lawn", "lawsuit",
        "layer", "lazy", "lead", "leaf", "learn", "lease", "least", "leather", "lecture", "left", "leg",
        "legal", "lemon", "level", "lever", "liberty", "library", "license", "life", "lift", "light",
        "like", "limb", "limit", "linen", "liner", "link", "lion", "list", "live", "liver", "living",
        "llama", "load", "loan", "lobby", "local", "lock", "lodge", "logic", "lonely", "loose", "lot",
        "lotus", "loud", "lounge", "love", "loyal", "lucky", "lumber", "lunar", "lunch", "lyric",
        "machine", "mad", "magic", "magnet", "maid", "mail", "main", "major", "make", "mammal", "man",
        "manage", "mandate", "mango", "mansion", "many", "maple", "march", "margin", "marine", "mark",
        "market", "marriage", "mask", "mass", "master", "match", "mate", "material", "math", "matter",
        "may", "maybe", "mayor", "meal", "mean", "means", "meanwhile", "measure", "meat", "mechanic",
        "medal", "media", "melon", "melt", "member", "memory", "mention", "menu", "mercy", "merge",
        "merit", "merry", "mesh", "mess", "message", "metal", "meter", "method", "middle", "midnight",
        "might", "milk", "million", "mimic", "mind", "mine", "minor", "minus", "mirror", "missile",
        "mission", "mist", "mistake", "mix", "mixed", "mixture", "mobile", "model", "modify", "moment",
        "money", "monitor", "monkey", "month", "moon", "moral", "more", "morning", "moss", "most",
        "mother", "motion", "motor", "mould", "mount", "mouse", "mouth", "move", "movie", "much",
        "mural", "murder", "music", "muscle", "museum", "mushroom", "must", "mutual", "myself", "mystery",
        "myth", "naive", "name", "napkin", "narrow", "nasty", "nation", "native", "nature", "near",
        "nearby", "nearly", "neck", "need", "needle", "negative", "neglect", "negotiate", "neighbor", "nerve",
        "nest", "never", "next", "nice", "night", "noble", "noise", "nominee", "none", "noon",
        "normal", "north", "notch", "note", "nothing", "notice", "notion", "novel", "now", "nuclear",
        "number", "nurse", "nutrition", "oak", "obey", "object", "obtain", "occupy", "occur", "ocean",
        "offer", "office", "often", "olive", "olympic", "omit", "once", "one", "onion", "online",
        "only", "open", "opera", "opinion", "oppose", "option", "orange", "orbit", "order", "organ",
        "orient", "original", "other", "outdoor", "outer", "output", "outside", "oval", "oven", "over",
        "own", "owner", "oxide", "oxygen", "oyster", "ozone", "paddle", "page", "paid", "pain",
        "paint", "pair", "palace", "palm", "panda", "panel", "panic", "paper", "parade", "parent",
        "park", "parrot", "party", "pass", "past", "paste", "patch", "path", "patient", "patrol",
        "patience", "pattern", "pause", "pay", "peace", "peach", "peacock", "pearl", "penny", "people",
        "pepper", "percent", "perfect", "perform", "perhaps", "period", "permit", "person", "pet", "petal",
        "petrol", "phase", "phone", "photo", "phrase", "physical", "piano", "pick", "picture", "piece",
        "pilot", "pin", "pine", "pink", "pioneer", "pipe", "pirate", "pistol", "pitch", "pizza",
        "place", "plain", "plan", "plane", "planet", "plant", "plate", "platform", "play", "please",
        "pledge", "pluck", "plot", "plow", "plug", "plunge", "point", "poison", "polar", "pizza",
        "police", "policy", "polish", "pollution", "pond", "pony", "pool", "popular", "portion", "position",
        "positive", "possible", "post", "pot", "potato", "potion", "powder", "power", "practice", "praise",
        "predict", "prefer", "premise", "premium", "prepare", "present", "preserve", "press", "pressure", "price",
        "pride", "priest", "primary", "print", "priority", "prison", "private", "prize", "problem", "process",
        "produce", "product", "profession", "profile", "profit", "program", "project", "promise", "promote", "proof",
        "property", "proposal", "propose", "prospect", "protect", "protein", "protest", "proud", "prove", "provide",
        "prowl", "prune", "public", "pull", "pulse", "pumpkin", "punch", "punishment", "pupil", "purchase",
        "pure", "purple", "purpose", "purse", "pursue", "push", "put", "puzzle", "pyramid", "quality",
        "quantum", "quarter", "queen", "query", "quest", "quick", "quiet", "quite", "quota", "quote",
        "rabbit", "race", "racoon", "rack", "radar", "radio", "raft", "rail", "rain", "rainbow",
        "raise", "rally", "ramp", "ranch", "random", "range", "rapid", "rare", "rash", "rate",
        "rather", "raven", "raw", "reach", "react", "read", "ready", "realm", "reap", "rear",
        "reason", "rebel", "recall", "receive", "recipe", "record", "recover", "reduce", "reflect", "reform",
        "refuse", "regard", "region", "regret", "regular", "reject", "relate", "relax", "release", "relief",
        "remain", "remark", "remedy", "remember", "remind", "remote", "remove", "render", "renew", "rent",
        "reopen", "repair", "repeat", "replace", "report", "rescue", "resemble", "resist", "resolve", "resort",
        "resource", "response", "rest", "result", "retire", "retreat", "return", "reveal", "review", "reward",
        "rhythm", "ribbon", "rice", "rich", "ride", "ridge", "rifle", "right", "rigid", "ring",
        "riot", "ripe", "rise", "risk", "ritual", "river", "road", "roast", "robot", "robust",
        "rocket", "rock", "rode", "role", "roll", "roof", "room", "root", "rope", "rose",
        "rotate", "rough", "round", "route", "royal", "rubber", "rugby", "rule", "rumor", "rural",
        "sad", "saddle", "sadness", "safe", "sail", "sailor", "salad", "salary", "salmon", "salon",
        "salt", "salute", "same", "sample", "sand", "satisfy", "satoshi", "sauce", "save", "say",
        "scale", "scan", "scare", "scarce", "scene", "scent", "scheme", "school", "science", "scissors",
        "scorpion", "scout", "scrap", "screen", "script", "scroll", "seal", "search", "season", "seat",
        "second", "secret", "section", "secure", "seed", "seek", "seem", "segment", "sell", "senate",
        "senior", "sense", "sentence", "series", "servant", "serve", "service", "session", "settle", "setup",
        "seven", "shadow", "shaft", "shake", "shall", "shame", "shape", "share", "shark", "sharp",
        "shed", "shell", "shelter", "shift", "shine", "ship", "shirt", "shock", "shoe", "shoot",
        "shop", "shore", "short", "shout", "show", "shower", "shrimp", "shrink", "shrug", "shrub",
        "shut", "sibling", "sick", "side", "siege", "sight", "sign", "signal", "silent", "silk",
        "silly", "silver", "similar", "simple", "since", "sing", "singer", "single", "sink", "site",
        "situation", "six", "size", "skate", "skill", "skin", "skirt", "skull", "slave", "sleep",
        "slice", "slide", "slight", "slim", "slogan", "slot", "slow", "small", "smart", "smell",
        "smile", "smoke", "snake", "snap", "snow", "so", "soap", "soccer", "social", "socket",
        "soft", "solar", "soldier", "solid", "solution", "solve", "some", "somebody", "someone", "something",
        "sometimes", "somewhat", "song", "soon", "sophisticated", "sorry", "sort", "soul", "sound",
        "soup", "source", "south", "space", "speak", "speaker", "special", "speed", "spell", "spend",
        "sphere", "spice", "spider", "spike", "spin", "spine", "spirit", "split", "spoke", "spoon",
        "sport", "spot", "spread", "spring", "spy", "square", "stable", "stadium", "staff", "stage",
        "stain", "stair", "stake", "stamp", "stand", "staple", "star", "start", "state", "station",
        "stay", "steak", "steal", "steam", "steel", "steep", "steer", "stem", "step", "stew",
        "stick", "still", "stock", "stomach", "stone", "stop", "storage", "store", "storm", "story",
        "stove", "straight", "strain", "strand", "strange", "stranger", "strap", "strategy", "straw", "strawberry",
        "stream", "street", "strength", "stress", "stretch", "strict", "stride", "strike", "string", "strip",
        "stripe", "strive", "stroke", "strong", "struggle", "stubborn", "student", "stuff", "stumble", "style",
        "subject", "submit", "subtle", "suburb", "success", "such", "suck", "sudden", "suffer", "sugar",
        "suggest", "suit", "summer", "summit", "sun", "sung", "sunlight", "sunrise", "sunset", "super",
        "supply", "suppose", "supreme", "sure", "surface", "surge", "surprise", "surround", "survey", "survival",
        "suspect", "sustain", "swallow", "swamp", "swarm", "swear", "sweat", "sweep", "sweet", "swift",
        "swim", "swing", "switch", "sword", "symbol", "symptom", "system", "table", "tablet", "tackle",
        "tail", "take", "talent", "talk", "tall", "tame", "tank", "tape", "target", "task", "taste",
        "tattoo", "taxi", "teach", "teacher", "team", "tear", "tease", "technical", "technique", "technology",
        "teenage", "teeth", "television", "temple", "tempt", "tenant", "tender", "tennis", "tense", "tension",
        "tent", "term", "test", "testify", "text", "thank", "that", "them", "theme", "then", "theory",
        "therapy", "there", "these", "they", "thick", "thief", "thigh", "thing", "think", "third",
        "thirteen", "thirty", "this", "though", "thought", "thousand", "thread", "threat", "threaten", "three",
        "thrill", "thrive", "throat", "throne", "throw", "thumb", "thunder", "thus", "tick", "ticket",
        "tide", "tiger", "tight", "timer", "tilt", "time", "timid", "tissue", "title", "toast",
        "today", "token", "tomato", "tomorrow", "tone", "tongue", "tonight", "tool", "tooth", "topic",
        "top", "torch", "total", "touch", "tough", "tour", "tourist", "tournament", "toward", "towards",
        "tower", "town", "toy", "track", "trade", "traffic", "tragic", "trail", "train", "trait",
        "transfer", "transform", "transit", "trash", "travel", "treat", "treaty", "tree", "tremendous", "trend",
        "trial", "tribe", "trick", "trigger", "trillion", "trim", "trip", "trophy", "tropical", "trouble",
        "truck", "truly", "trumpet", "trust", "truth", "tunnel", "turkey", "turn", "turtle", "twelve",
        "twenty", "twice", "twin", "twist", "two", "type", "typical", "ugly", "ultimate", "umbrella",
        "unable", "uncle", "under", "undergo", "understand", "undo", "unfair", "unfold", "unhappy", "uniform",
        "union", "unique", "unit", "unite", "unity", "universe", "unknown", "unless", "unlike", "unlock",
        "until", "update", "upgrade", "uphold", "upon", "upper", "upset", "urban", "urge", "usage",
        "use", "used", "useful", "useless", "usual", "utility", "vaccine", "vacuum", "vague", "valid",
        "valley", "value", "valve", "vampire", "van", "vanish", "various", "vast", "vault", "venue",
        "verse", "version", "very", "vessel", "veteran", "viable", "vibrant", "vicious", "victory", "video",
        "view", "village", "vintage", "violin", "virtual", "virus", "visible", "vision", "visit", "visual",
        "vital", "vivid", "vocal", "voice", "void", "volcano", "volume", "vote", "voyage", "wage",
        "wagon", "wait", "wake", "walk", "wall", "wander", "want", "war", "warm", "warn",
        "warrant", "warrior", "wash", "waste", "watch", "water", "wave", "weak", "wealth", "wealthy",
        "weapon", "wear", "weasel", "weather", "web", "wedding", "weekend", "weight", "weird", "welcome",
        "well", "west", "western", "whale", "what", "whatever", "wheat", "wheel", "when", "where",
        "whether", "which", "while", "whisper", "whistle", "white", "whole", "whom", "whose", "wicked",
        "wide", "width", "wife", "wild", "will", "win", "wind", "window", "wine", "wing",
        "wink", "winner", "winter", "wire", "wisdom", "wise", "wish", "witness", "wolf", "woman",
        "wonder", "wood", "wool", "word", "work", "world", "worry", "worth", "would", "wound",
        "wrap", "wrath", "wreck", "wrestle", "wrist", "write", "wrong", "yard", "year", "yellow",
        "yesterday", "yield", "young", "youth", "zebra", "zero", "zone", "zoo"
    };

    public static string[] WordList => _wordList;

    public static string GetWord(int index)
    {
        if (index < 0 || index >= _wordList.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _wordList[index];
    }

    public static int GetWordIndex(string word)
    {
        var index = Array.IndexOf(_wordList, word.ToLowerInvariant());
        if (index < 0)
            throw new ArgumentException($"Word '{word}' not found in wordlist");
        return index;
    }
}
