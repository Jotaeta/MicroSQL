using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StorageMicroSQL;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MicroSQL.Models
{
    [Serializable]
    public class Tabla : ArbolBMas
    {
        public const string rutaTablas = @"arbolesB\";
        public const string extTabla = ".arbolb";

        public string nombre;
        public Dictionary<string, string> columnas;

        public Tabla(string nombre, Dictionary<string, string> columnas)
        {
            this.nombre = nombre;
            this.columnas = columnas;
        }        

        public bool guardar()
        {
            FileStream fs = File.OpenWrite(this.ruta());                        
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize( fs, this);
            fs.Close();
            return true;
        }        

        private string ruta()
        {
            return Tabla.rutaTablas + this.nombre + Tabla.extTabla;
        }

        public static Tabla cargarTabla(string rutaArchivo)
        {
            FileStream fs = File.OpenRead(rutaArchivo);            
            BinaryFormatter b = new BinaryFormatter();
            Tabla tabla = (Tabla) b.Deserialize(fs);
            fs.Close();
            return tabla;
        }

    }
}
