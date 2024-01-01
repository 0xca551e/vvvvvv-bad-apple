using System.Xml;
using SixLabors.ImageSharp;

if (args.Length != 2) {
    Console.WriteLine("Usage: script-generator <path to frames> <path to .vvvvvv level>");
    return;
}

var folderPath = args[0];
var xmlPath = args[1];

const int offsetX = 48;
const int offsetY = 16;
var files = Directory.GetFiles(folderPath);
Array.Sort(files);

var scripts = new List<string>();

foreach(var script in createTimelineScripts(files.Length)) {
    scripts.Add(script.ToInternal().ToString());
}

var s = createRenderScriptsFromFolder(folderPath);
foreach(var script in s) {
    scripts.Add(script.ToInternal().ToString());
}

foreach(var script in createTimerScripts(files.Length)) {
    scripts.Add(script.ToInternal().ToString());
}

var resetScript = new Script("reset", ["iftrinkets(0,_reset)"]);
scripts.Add(resetScript.ToString());

var maxDigits = Convert.ToInt32(Convert.ToString(files.Length, 2).Length);
var resetInternalScript = new Script("_reset", []);
    resetInternalScript.ScriptContents.Add("musicfadeout()");
for (int i = 0; i < maxDigits; i++) {
    resetInternalScript.ScriptContents.Add($"flag({i},off)");
}
scripts.Add(resetInternalScript.ToInternal().ToString());

scripts.Add(new Script("render", [
    $"iftrinkets(0,render_{new string('x', maxDigits)})"
]).ToString());

scripts.Add(new Script("info", [
    "say(4)",
    "Bad Apple!! in VVVVVV",
    "By 0xca551e",
    "Go to the room on the",
    "right to start"
]).ToString());

scripts.Add(new Script("endinfo", [
    "say(1)",
    "Thanks for watching!"
]).ToString());

scripts.Add(new Script("room_3_0", [
    "iftrinkets(0,_room_3_0)"
]).ToString());

scripts.Add(new Script("_room_3_0", [
    "changecustommood(red,0)",
]).ToInternal().ToString());


var doc = new XmlDocument();
doc.Load(xmlPath);
var root = doc.DocumentElement!;
var scriptNode = root.SelectSingleNode("descendant::script")!;
scriptNode.InnerText = string.Join('|', scripts);
var tw = new XmlTextWriter(xmlPath, null);  
doc.Save(tw);

static List<string> createRenderScriptContentsFromImage(string filePath) {
    var result = new List<string>();
    var startX = -1;
    void flush(int x, int y, int w) {
        result.Add($"createentity({x + offsetX},{y + offsetY},11,{w})");
        startX = -1;
    }

    var image = Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(filePath);
    for (int y = 0; y < image.Height; y++) {
        for (int x = 0; x < image.Width; x++) {
            var pixel = image[x,y];
            var red = pixel.R;
            if (red == 255 && startX == -1) {
                startX = x;
            } else if (red == 0 && startX != -1) {
                flush(startX, y, x - startX + 1);
            }
        }
        if (startX != -1) {
            flush(startX, y, image.Width - startX);
        }
    }
    result.Add("customiftrinkets(0,increment_0)");

    return result;
}

static List<Script> createRenderScriptsFromFolder(string folderPath) {
    var result = new List<Script>();
    var files = Directory.GetFiles(folderPath);
    Array.Sort(files);
    var maxDigits = Convert.ToInt32(Convert.ToString(files.Length, 2).Length);
    for (var i = 0; i < files.Length; i++) {
        var script = new Script($"render_{Convert.ToString(i, 2).PadLeft(maxDigits,'0')}", []);
        if (i == 0) {
            script.ScriptContents.Add("play(16)");
        } else if (i == files.Length - 1) {
            script.ScriptContents.Add("stopmusic()");
            script.ScriptContents.Add("gotoroom(2,0)");
        }
        foreach (var x in createRenderScriptContentsFromImage(files[i])) {
            script.ScriptContents.Add(x);
        }
        result.Add(script);
    }
    return result;
}

static List<Script> createTimerScripts(int totalFrames) {
    var result = new List<Script>();
    var maxDigits = Convert.ToInt32(Convert.ToString(totalFrames, 2).Length);
    for (int i = 0; i < maxDigits; i++) {
        var script = new Script($"increment_{i}", [
            $"customifflag({i}, increment_{i + 1})",
            $"flag({i},on)"
        ]);
        for (int j = i - 1; j >= 0; j--) {
            script.ScriptContents.Add($"flag({j},off)");
        }
        result.Add(script);
    }
    return result;
}

static List<Script> createTimelineScripts(int totalFrames) {
    var result = new List<Script>();
    var maxDigits = Convert.ToInt32(Convert.ToString(totalFrames, 2).Length);
    recurse("");
    return result;

    void recurse(string s) {
        if (s.Length == maxDigits) {
            return;
        }

        var bitString = s.PadLeft(maxDigits, 'x');
        var zeroPlusBitString = ('0' + s).PadLeft(maxDigits, 'x');
        var onePlusBitString = ('1' + s).PadLeft(maxDigits, 'x');
        var name = $"render_{bitString}";
        var script = new Script(name, []);
        if (s == "") {
            script.ScriptContents.Add($"gotoroom(1,0)");
        }
        script.ScriptContents.Add($"customifflag({s.Length},{$"render_{onePlusBitString}"})");
        script.ScriptContents.Add($"customiftrinkets(0,{$"render_{zeroPlusBitString}"})");
        result.Add(script);
        
        recurse('0' + s);
        recurse('1' + s);
    }
}

class Script(string scriptName, List<string> scriptContents)
{
    public string ScriptName { get; } = scriptName;
    public List<string> ScriptContents { get; set; } = scriptContents;

    public Script ToInternal()
    {
        var result = new List<string>();

        var withStopScript = new List<string>(this.ScriptContents);
        var originalLastLine = this.ScriptContents.Last();

        if (!originalLastLine.StartsWith("iftrinkets(0,") &&
            !originalLastLine.StartsWith("customiftrinkets(0,") &&
            !originalLastLine.StartsWith("loadscript"))
        {
            withStopScript.Add("loadscript(stop)");
        }

        result.Add("squeak(off)");
        var chunks = withStopScript.Chunk(49).ToList();
        for(int i = 0; i < chunks.Count; i++) {
            var offset = i == chunks.Count - 1 ? 0 : 1;
            result.Add($"say({chunks[i].Length + offset})");
            foreach(var item in chunks[i]) {
                result.Add(item);
            }
            result.Add("text(1,0,0,3)");
        }

        return new Script(this.ScriptName, result);
    }

    public override string ToString()
    {
        return this.ScriptName + ":|" + String.Join('|', this.ScriptContents);
    }
}
