using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DocImporter.Models;
using DocImporter.Services;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using DocImporter.Data;

namespace DocImporter.Controllers
{
    public class HomeController : Controller
    {
        private readonly DocToTextService _docToTextService;
        private readonly AppDbContext _dbContext;

        public HomeController(DocToTextService docToTextService, AppDbContext dbContext)
        {
            _docToTextService = docToTextService;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Upload([FromForm] UploadBindingModel model)
        {
            if (model.File == null || model.File.Length == 0) return BadRequest("Please upload a file.");

            Stream stream = new MemoryStream();

            try
            {
                using var reader = new StreamReader(model.File.OpenReadStream());
                await model.File.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var text = _docToTextService.ReadAllTextFromDocx(stream);
            var poDocTextList = text.Split(new char[] { '\n', '\r', '\u2028' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var curatedMusiclist = new List<CuratedMusic>();

            bool hasMore = true;
            var index = 0;
            var dicList = new List<Dictionary<string, string>>();
            Dictionary<string, string> dic;

            try
            {
                do
                {
                    index = poDocTextList.FindIndex(index, x => x.Contains("Curator"));
                    if (index <= 0) break;
                    var nextCurator = poDocTextList.FindIndex(index + 1, x => x.Contains("Curator"));
                    if (nextCurator <= 0)
                    {
                        nextCurator = poDocTextList.Count;
                        hasMore = false;
                    }

                    dic = new Dictionary<string, string>();

                    var title = poDocTextList[index - 1];
                    if (!string.IsNullOrEmpty(title)) dic.Add("Title", title);

                    do
                    {
                        var line = poDocTextList[index];
                        var fields = Regex.Matches(line, @"[a-zA-Z]*[ ]?(:)(?!\/)").Select(x => x.Value).ToArray();
                        if (fields.Count() == 0)
                        {
                            index++;
                            continue;
                        }

                        var lineArray = line.Split(fields, StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length != fields.Length)
                        {
                            lineArray = lineArray.Where(x => !x.Contains("Spotify Playlist", StringComparison.OrdinalIgnoreCase)).ToArray();
                        }

                        for (var i = 0; i < fields.Length; i++)
                        {
                            string key = fields[i].Contains("Page", StringComparison.OrdinalIgnoreCase) ? "SpotifyPlaylistPage" : fields[i].Replace(":", "").Trim();
                            string value = lineArray[i].Trim();

                            if(!dic.Keys.Any(x => x == key)) dic.Add(key, value);
                        }

                        index++;

                    } while (nextCurator > index);

                    dicList.Add(dic);

                } while (hasMore);

                curatedMusiclist = JsonConvert.DeserializeObject<List<CuratedMusic>>(JsonConvert.SerializeObject(dicList));

                if (curatedMusiclist.Any())
                {
                    await _dbContext.AddRangeAsync(curatedMusiclist);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
