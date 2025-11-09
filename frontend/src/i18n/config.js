import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import translationES from './es/translation.json';
import translationEN from './en/translation.json';

const resources = {
    es: {
        translation: translationES
    },
    en: {
        translation: translationEN
    }
};

i18n
    .use(LanguageDetector) // Detecta el idioma del navegador
    .use(initReactI18next) // Pasa i18n a react-i18next
    .init({
        resources,
        fallbackLng: 'es', // Idioma por defecto: español
        lng: localStorage.getItem('i18nextLng') || 'es', // Usa el idioma guardado o español por defecto
        
        interpolation: {
            escapeValue: false // React ya escapa los valores
        },
        
        detection: {
            // Orden de detección del idioma
            order: ['localStorage', 'navigator', 'htmlTag'],
            // Guarda el idioma seleccionado en localStorage
            caches: ['localStorage'],
            // Clave en localStorage
            lookupLocalStorage: 'i18nextLng'
        }
    });

export default i18n;

