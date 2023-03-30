﻿using ASP_201.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ASP_201.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Intro()
        {
            return View();
        }
        public IActionResult Scheme() 
        {
            ViewBag.bagdata = "Data in ViewBag";     // Способи передачі даних
            ViewData["data"] = "Data in ViewData";   // до представлення

            return View();
        }
        public IActionResult Razor()
        {
            return View();
        }
        public IActionResult PassData()
        {
            Models.Home.PassDataModel model = new()
            {
                Header = "Моделі",
                Title = "Моделі передачі даних",
                Products = new()
                {
                    new() { Name = "Зарядний кабель",       Price = 210    },
                    new() { Name = "Маніпулятор 'миша'",    Price = 399.50 },
                    new() { Name = "Наліпка 'Smiley'",      Price = 2.95   },
                    new() { Name = "Серветки для монітору", Price = 100    },
                    new() { Name = "USB ліхтарик",          Price = 49.50  },
                    new() { Name = "Аккумулятор ААА",       Price = 280    },
                    new() { Name = "ОС Windows Home",       Price = 1250   },
                }
            };

            return View(model);
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
    }
}