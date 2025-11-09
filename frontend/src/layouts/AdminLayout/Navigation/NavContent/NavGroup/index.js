import React from 'react';
import { useTranslation } from 'react-i18next';
import { ListGroup } from 'react-bootstrap';
import NavCollapse from '../NavCollapse';
import NavItem from '../NavItem';

const NavGroup = ({ layout, group }) => {
    const { t } = useTranslation();
    let navItems = '';

    if (group.children) {
        const groups = group.children;
        navItems = Object.keys(groups).map((item) => {
            item = groups[item];
            switch (item.type) {
                case 'collapse':
                    return <NavCollapse key={item.id} collapse={item} type="main" />;
                case 'item':
                    return <NavItem layout={layout} key={item.id} item={item} />;
                default:
                    return false;
            }
        });
    }

    // Si el título es una clave de traducción (contiene un punto), traducirlo
    const groupTitle = group.title.includes('.') ? t(group.title) : group.title;

    return (
        <React.Fragment>
            <ListGroup.Item as="li" bsPrefix=" " key={group.id} className="nav-item pcoded-menu-caption">
                <label>{groupTitle}</label>
            </ListGroup.Item>
            {navItems}
        </React.Fragment>
    );
};

export default NavGroup;
