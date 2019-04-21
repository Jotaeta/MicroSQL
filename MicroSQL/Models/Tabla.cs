using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StorageMicroSQL;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using MicroSQL.Excepciones;

namespace MicroSQL.Models
{
    [Serializable]
    public class Tabla : ArbolBMas
    {
        public const string rutaTablas = @"arbolesB\";
        public const string extTabla = ".arbolb";

        public string nombre;
        public string llavePrimaria;
        public Dictionary<string, string> columnas;

        public Tabla(string nombre, Dictionary<string, string> columnas, string llavePrimaria)
        {
            this.nombre = nombre;
            this.columnas = columnas;
            this.llavePrimaria = llavePrimaria;
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
            return Tabla.rutaTabla(this.nombre);
        }

        public static string rutaTabla(string nombreTabla)
        {
            return Tabla.rutaTablas + nombreTabla + Tabla.extTabla;
        }

        public static Tabla cargarTabla(string rutaArchivo)
        {
            FileStream fs = File.OpenRead(rutaArchivo);            
            BinaryFormatter b = new BinaryFormatter();
            Tabla tabla = (Tabla) b.Deserialize(fs);
            fs.Close();
            return tabla;
        }

        public static bool borrarTabla(string rutaArchivo)
        {
            if (File.Exists(rutaArchivo)) {
                File.Delete(rutaArchivo);
                return true;
            }else
            {
                return false;
            }
            
        }

        public string insertar(Dictionary<string, string> data)
        {
            this.validarDatosAInsertar(data);
            string llaveStr = data.GetValueOrDefault(this.llavePrimaria);
            int llave = int.Parse(llaveStr);
            NodoArbolBMas nuevoRegistro = this.nuevoNodo(llave, data);
            if (this.insertar(nuevoRegistro) != null)
            {
                return "Nuevo registro insertado con exito";
            }
            else
            {
                return "Error al insertar";
            };
        }

        private bool validarDatosAInsertar(Dictionary<string, string> data)
        {
            string columna, valor;
            for(int i = 0; i< data.Count; i++)
            {
                columna = data.ElementAt(i).Key;
                valor = data.ElementAt(i).Value;

                this.validarColumnaValor(columna, valor);
                
            }
            return true;
        }

        private void validarColumnaValor(string columna, string valor)
        {
            string tipo;
            if ((tipo = this.columnas.GetValueOrDefault(columna)) == null)
            {
                throw new ErrorDeSemantica("No existe la columna " + columna + " en la tabla " + this.nombre);
            }
            this.validarTipoValor(tipo, valor);
        }

        private bool validarTipoValor(string tipo, string valor)
        {
            string regex = "";
            tipo = tipo.Replace("primary key", "");
            tipo = tipo.Contains("varchar") ? "varchar" : tipo;            

            switch (tipo.Trim())
            {
                case "int": regex = @"^\d+$"; break;
                case "varchar": regex = @"^\'[^']*\'$"; break;
                case "datetime": regex = "^'[0-9][0-9]/[0-9][0-9]/[0-9][0-9][0-9][0-9]'$"; break;
                default:
                    throw new Exception("TIPO DESCONOCIDO");
            }
            if (Regex.Match(valor, regex).Success)
            {
                return true;
            }
            else
            {
                throw new ErrorDeSemantica("Valor [" + valor + "] incorrecto para columna tipo " + tipo);
            }
        }

        public List<object> select(ref List<string> columnas, string columnaFiltro, string valorFiltro)
        {
            List<object> dataset = new List<object>();
            List<NodoArbolBMas> listadoDeNodos = this.obtenerListadoNodos(columnas, columnaFiltro, valorFiltro);
            columnas = columnas == null ? this.columnas.Keys.ToList<string>() : columnas;

            //MAPEAR A REUSLTADO FINAL
            for (int i=0; i< listadoDeNodos.Count; i++)
            {
                NodoArbolBMas nodo = listadoDeNodos.ElementAt(i);
                Dictionary<string, string> row = new Dictionary<string, string>();
                for (int j = 0; j < columnas.Count; j++)
                {
                    row.Add(columnas[j], nodo.data.GetValueOrDefault(columnas[j]));
                }
                dataset.Add(row);
            }
            return dataset;            
        }

        private List<NodoArbolBMas> obtenerListadoNodos(List<string> columnas, string columnaFiltro, string valorFiltro)
        {
            List<NodoArbolBMas> listadoDeNodos = new List<NodoArbolBMas>();
            columnas = columnas == null ? this.columnas.Keys.ToList<string>() : columnas;

            //SELECT CON WHERE
            if (columnaFiltro != null || valorFiltro != null)
            {
                //VALIDAR COLUMNA Y VALOR DE BUSQUEDA
                this.validarColumnaValor(columnaFiltro, valorFiltro);
            }

            //BUSCANDO POR LLAVE PRIMARIA
            if (columnaFiltro == this.llavePrimaria)
            {
                NodoArbolBMas nodo = this.buscarPorLlave(int.Parse(valorFiltro));
                if (nodo != null)
                    listadoDeNodos.Add(nodo);
            }
            else
            {
                listadoDeNodos = this.buscarEnTodo(columnaFiltro, valorFiltro);
            }
            return listadoDeNodos;
        }

        public int delete(string columnaFiltro, string valorFiltro)
        {
            List<object> dataset = new List<object>();
            List<string> columnas = null;
            List<NodoArbolBMas> listadoDeNodos = this.obtenerListadoNodos(columnas, columnaFiltro, valorFiltro);

            int resultado = 0;
            //MAPEAR A REUSLTADO FINAL
            for (int i = 0; i < listadoDeNodos.Count; i++)
            {
                NodoArbolBMas nodo = listadoDeNodos.ElementAt(i);
                if(this.eliminar(nodo)) resultado++;
            }
            return resultado;
        }
    }
}
