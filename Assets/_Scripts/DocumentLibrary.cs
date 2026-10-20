using UnityEngine;

/// <summary>
/// Catalogo de documentos del juego (mapa, foto, diario, nota): imagen, titulo y transcripcion.
/// Cada pista encuentra su documento por el nombre de su objeto (Pista_Mapa, Pista_Foto...),
/// asi no depende de referencias guardadas en la escena.
/// Las imagenes estan en Assets/Resources/Documentos/.
/// </summary>
public static class DocumentLibrary
{
    static readonly string[] Keys = { "Mapa", "Foto", "Diario", "Nota" };

    /// <summary>Devuelve la clave del documento de este objeto, o null si no es un documento.</summary>
    /// <param name="t">Objeto de la pista.</param>
    public static string KeyFor(Transform t)
    {
        if (t == null) return null;
        string k = Match(t.name);
        if (k != null) return k;
        foreach (Transform c in t.GetComponentsInChildren<Transform>(true))
        {
            k = Match(c.name);
            if (k != null) return k;
        }
        return null;
    }

    static string Match(string objName)
    {
        string n = objName.ToLowerInvariant();
        if (!n.StartsWith("pista_")) return null;
        foreach (var k in Keys) if (n == "pista_" + k.ToLowerInvariant()) return k;
        return null;
    }

    /// <summary>Carga el documento.</summary>
    public static bool TryGet(string key, out Texture2D image, out string title, out string text)
    {
        image = key != null ? Resources.Load<Texture2D>("Documentos/" + key) : null;
        title = Title(key);
        text = Transcript(key);
        return image != null;
    }

    /// <summary>Titulo del documento.</summary>
    public static string Title(string key)
    {
        switch (key)
        {
            case "Mapa": return "Mapa del bosque";
            case "Foto": return "Fotografía familiar";
            case "Diario": return "Página de diario";
            case "Nota": return "Nota arrugada";
            case "Registro": return "Registro del Proyecto Hollow";
        }
        return key;
    }

    /// <summary>Transcripcion del texto del documento.</summary>
    public static string Transcript(string key)
    {
        switch (key)
        {
            case "Mapa":
                return "Un mapa dibujado a mano del bosque.\n\n" +
                       "Un camino sale del laboratorio hacia el norte, hasta la Torre 7, la de la luz roja. " +
                       "Desde la torre, un sendero va al este, a la casa vieja, y otro al noroeste, a una cabaña.\n\n" +
                       "La cabaña está marcada con una X roja.";
            case "Foto":
                return "Una fotografía vieja de una familia frente a una cabaña en el bosque: un hombre, una mujer y dos niños.\n\n" +
                       "Alguien rayó la cara de la mujer con tinta roja.\n\n" +
                       "Abajo, escrito a mano: «Mayo 2019».";
            case "Diario":
                return "14 de junio de 2019\n" +
                       "CRAYO nos prometió que era un proyecto seguro. Un lugar aislado, la oportunidad perfecta. Ahora sé que no debí venir. " +
                       "Escuché voces anoche. No eran animales.\n\n" +
                       "17 de junio de 2019\n" +
                       "Encontré los registros del laboratorio. Los experimentos de CRAYO no fallaron. Crearon algo. Algo que escucha.\n\n" +
                       "20 de junio de 2019\n" +
                       "Se llevaron a [tachado] y a los niños. Desperté y ya no estaban. La puerta estaba abierta. No hay huellas.\n\n" +
                       "22 de junio de 2019\n" +
                       "Escondí el registro en la cabaña del bosque. Dejé la llave en la torre. " +
                       "Si alguien encuentra esto: saca el registro. Que todos sepan lo que hizo CRAYO.\n\n" +
                       "Me escucha. Ya viene.";
            case "Registro":
                return "CRAYO BIOSYSTEMS — PROYECTO HOLLOW\n" +
                       "Instalación: Laboratorio Bosque / Torre 7\n" +
                       "Clasificación: CONFIDENCIAL\n\n" +
                       "03/03/2019\n" +
                       "El organismo H-7 sobrevive fuera del tanque. No tiene ojos. Se guía por el sonido: " +
                       "cuanto más silencio hacemos, más tarda en encontrarnos.\n\n" +
                       "28/05/2019\n" +
                       "Primera unión con un huésped humano. Voluntaria: [CENSURADO], investigadora del proyecto. " +
                       "El huésped conserva recuerdos. Por las noches camina hacia la casa vieja y se queda mirando la puerta.\n\n" +
                       "20/06/2019\n" +
                       "Contención perdida. H-7 salió del laboratorio. Dos menores desaparecidos en el perímetro. " +
                       "No hay rastros. Solo la puerta abierta.\n\n" +
                       "22/06/2019\n" +
                       "La dirección ordena activar el PROTOCOLO HOLLOW: sellar el laboratorio, borrar los registros " +
                       "y retirar al personal. Los residentes del bosque no serán evacuados. Esta instalación nunca existió.\n\n" +
                       "Nota a mano, al final de la hoja:\n" +
                       "«No la maten. Todavía es ella. Todavía nos escucha. — R.»";
            case "Nota":
                return "La llave de la cabaña está en la torre.\nSigue la luz roja.\nNo dejes que te escuche.\n\nNO CORRAS.";
        }
        return "";
    }
}
