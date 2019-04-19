using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MicroSQL.Models;
using System.IO;
using System.Text;
using MicroSQL.Classes;

namespace MicroSQL.Controllers
{
	public class HomeController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult About()
		{
			ViewData["Message"] = "Your application description page.";

			return View();
		}

		public IActionResult Contact()
		{
			ViewData["Message"] = "Your contact page.";

			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

        [HttpPost]
        public ActionResult Ejecutar(string instruccion)
        {
            Interprete interprete = new Interprete();            
            return Json(interprete.ejecutar(instruccion));
        }

        public ActionResult getTablas()
        {
            return Json(this.cargarTablas());
        }

        private List<Tabla> cargarTablas()
        {

            bool exists = Directory.Exists(Tabla.rutaTablas);

            if (!exists)
                Directory.CreateDirectory(Tabla.rutaTablas);

            DirectoryInfo d = new DirectoryInfo(Tabla.rutaTablas);
            List<Tabla> tablas = new List<Tabla>();
            foreach (var file in d.GetFiles(@"*" + Tabla.extTabla))
            {
                tablas.Add(Tabla.cargarTabla(file.FullName));
            }
            return tablas;            
        }
	}
}
