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
                    title: 'menu.courses',
                    type: 'item',
                    url: '/app/courses',
                    icon: 'feather icon-book'
                },
                {
                    id: 'enrollments',
                    title: 'menu.enrollments',
                    type: 'item',
                    url: '/app/enrollments',
                    icon: 'feather icon-file-text'
                },
                {
                    id: 'calendar',
                    title: 'menu.calendar',
                    type: 'item',
                    url: '/app/calendar',
                    icon: 'feather icon-calendar'
                },
                {
                    id: 'teachers',
                    title: 'menu.teachers',
                    type: 'item',
                    url: '/app/teachers',
                    icon: 'feather icon-users'
                },
                {
                    id: 'certificates',
                    title: 'menu.certificates',
                    type: 'item',
                    url: '/app/certificates',
                    icon: 'feather icon-award'
                },
                {
                    id: 'grades',
                    title: 'menu.grades',
                    type: 'item',
                    url: '/app/grades',
                    icon: 'feather icon-star'
                },
                {
                    id: 'messages',
                    title: 'menu.messages',
                    type: 'item',
                    url: '/app/messages',
                    icon: 'feather icon-message-circle'
                },
                {
                    id: 'profile',
                    title: 'menu.profile',
                    type: 'item',
                    url: '/app/profile',
                    icon: 'feather icon-user'
                }
            ]
        }
    ]
};

export default menuItems;
