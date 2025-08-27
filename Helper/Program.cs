using Q3MinimapGenerator;

Minimap.Generate("/in/path", new() {
	OutputFolderPath = "/out/path",
	MaxWidth = 1024,
	MaxHeight = 1024,
	ExtraBorderUnits = 10,
	AxisPlane = MiniMapAxisPlane.XY,
	ImageType = ImageType.GrayscaleA,
	MakeMeta = false,
	Predicate = (name, files) => {
		return files == null || files.Length <= 0 || files[0].Name == ".DS_Store"; //fuck mac
	},
	ImageFilePathFormatter = (path, axisName, meta) => {
		return Path.Combine(path, $"{axisName},{meta}.png");
	}
});