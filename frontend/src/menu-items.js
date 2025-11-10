const menuItems = {
    items: [
        {
            id: 'navigation',
            title: 'menu.navigation',
            type: 'group',
            icon: 'icon-navigation',
            children: [
                {
                    id: 'dashboard',
                    title: 'Inicio',
                    type: 'item',
                    url: '/app/dashboard/default',
                    icon: 'feather icon-home'
                },
                {
                    id: 'courses',
                    title: 'Materias',
                    type: 'item',
                    url: '/app/courses',
                    icon: 'feather icon-book'
                },
                {
                    id: 'enrollments',
                    title: 'Inscripciones',
                    type: 'item',
                    url: '/app/enrollments',
                    icon: 'feather icon-file-text'
                },
                {
                    id: 'calendar',
                    title: 'Calendario',
                    type: 'item',
                    url: '/app/calendar',
                    icon: 'feather icon-calendar'
                },
                {
                    id: 'teachers',
                    title: 'Profesores',
                    type: 'item',
                    url: '/app/teachers',
                    icon: 'feather icon-users'
                },
                {
                    id: 'certificates',
                    title: 'Certificados',
                    type: 'item',
                    url: '/app/certificates',
                    icon: 'feather icon-award'
                },
                {
                    id: 'grades',
                    title: 'Notas',
                    type: 'item',
                    url: '/app/grades',
                    icon: 'feather icon-star'
                },
                {
                    id: 'messages',
                    title: 'menu.messages',
                    type: 'item',
                    url: 'Foros',
                    icon: 'feather icon-message-circle'
                },
                {
                    id: 'profile',
                    title: 'Perfil',
                    type: 'item',
                    url: '/app/profile',
                    icon: 'feather icon-user'
                }
            ]
        }
    ]
};

export default menuItems;
