import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ListGroup, Dropdown, Media } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import PerfectScrollbar from 'react-perfect-scrollbar';
import ChatList from './ChatList';
import { useLogout } from '../../../../features/auth/application/useLogout';

import avatar1 from '../../../../assets/images/user/avatar-1.jpg';
import avatar2 from '../../../../assets/images/user/avatar-2.jpg';
import avatar3 from '../../../../assets/images/user/avatar-3.jpg';
import avatar4 from '../../../../assets/images/user/avatar-4.jpg';

const NavRight = () => {
    const { t, i18n } = useTranslation();
    const { logout } = useLogout();

    const [listOpen, setListOpen] = useState(false);

    const changeLanguage = (lng) => {
        i18n.changeLanguage(lng);
    };

    const handleLogout = () => {
        logout();
    };

    return (
        <React.Fragment>
            <ListGroup as="ul" bsPrefix=" " className="navbar-nav ms-auto" id="navbar-right">
                <ListGroup.Item as="li" bsPrefix=" ">
                    <Dropdown>
                        <Dropdown.Toggle as={Link} variant="link" to="#" id="dropdown-basic">
                            <i className="feather icon-bell icon" />
                        </Dropdown.Toggle>
                        <Dropdown.Menu alignRight className="notification notification-scroll">
                            <div className="noti-head">
                                <h6 className="d-inline-block m-b-0">{t('header.notifications')}</h6>
                                <div className="float-end">
                                    <Link to="#" className="m-e-10">
                                        {t('header.markAsRead')}
                                    </Link>
                                    <Link to="#">{t('header.clearAll')}</Link>
                                </div>
                            </div>
                            <PerfectScrollbar>
                                <ListGroup as="ul" bsPrefix=" " variant="flush" className="noti-body">
                                    <ListGroup.Item as="li" bsPrefix=" " className="n-title">
                                        <p className="m-b-0">{t('header.new')}</p>
                                    </ListGroup.Item>
                                    <ListGroup.Item as="li" bsPrefix=" " className="notification">
                                        <Media>
                                            <img className="img-radius" src={avatar1} alt="Generic placeholder" />
                                            <Media.Body>
                                                <p>
                                                    <strong>Jaz</strong>
                                                    <span className="n-time text-muted">
                                                        <i className="icon feather icon-clock m-e-10" />
                                                        30 min
                                                    </span>
                                                </p>
                                                <p>Solicitud Constancia de Estudios</p>
                                            </Media.Body>
                                        </Media>
                                    </ListGroup.Item>
                                    <ListGroup.Item as="li" bsPrefix=" " className="n-title">
                                        <p className="m-b-0">{t('header.earlier')}</p>
                                    </ListGroup.Item>
                                    <ListGroup.Item as="li" bsPrefix=" " className="notification">
                                        <Media>
                                            <img className="img-radius" src={avatar2} alt="Generic placeholder" />
                                            <Media.Body>
                                                <p>
                                                    <strong>Ramon</strong>
                                                    <span className="n-time text-muted">
                                                        <i className="icon feather icon-clock m-e-10" />
                                                        30 min
                                                    </span>
                                                </p>
                                                <p>Solicitud de Inscripcion</p>
                                            </Media.Body>
                                        </Media>
                                    </ListGroup.Item>
                                    <ListGroup.Item as="li" bsPrefix=" " className="notification">
                                        <Media>
                                            <img className="img-radius" src={avatar3} alt="Generic placeholder" />
                                            <Media.Body>
                                                <p>
                                                    <strong>Claudia</strong>
                                                    <span className="n-time text-muted">
                                                        <i className="icon feather icon-clock m-r-10" />
                                                        30 min
                                                    </span>
                                                </p>
                                                <p>Inscripcion de Catedra</p>
                                            </Media.Body>
                                        </Media>
                                    </ListGroup.Item>
                                    <ListGroup.Item as="li" bsPrefix=" " className="notification">
                                        <Media>
                                            <img className="img-radius" src={avatar4} alt="Generic placeholder" />
                                            <Media.Body>
                                                <p>
                                                    <strong>Prof.Pepito</strong>
                                                    <span className="n-time text-muted">
                                                        <i className="icon feather icon-clock m-e-10" />
                                                         Ayer
                                                    </span>
                                                </p>
                                                <p>Solicitud de Horarios</p>
                                            </Media.Body>
                                        </Media>
                                    </ListGroup.Item>
                                </ListGroup>
                            </PerfectScrollbar>
                            <div className="noti-footer">
                                <Link to="#">{t('header.showAll')}</Link>
                            </div>
                        </Dropdown.Menu>
                    </Dropdown>
                </ListGroup.Item>
                <ListGroup.Item as="li" bsPrefix=" ">
                    <Dropdown>
                        <Dropdown.Toggle as={Link} variant="link" to="#" className="displayChatbox" onClick={() => setListOpen(true)}>
                            <i className="icon feather icon-mail" />
                        </Dropdown.Toggle>
                    </Dropdown>
                </ListGroup.Item>


                <ListGroup.Item as="li" bsPrefix=" ">
                <Dropdown align="end" className="nav-lang-dropdown">
                    <Dropdown.Toggle
                    variant="link"
                    id="dropdown-language"
                    className="nav-icon-btn no-caret"
                    >
                    <div className="lang-icon">
                        <i className="feather icon-globe" />
                        <i className="feather icon-chevron-down caret-icon" />
                    </div>
                    </Dropdown.Toggle>

                    <Dropdown.Menu align="end" className="lang-menu">
                    <Dropdown.Item
                        onClick={() => changeLanguage('es')}
                        className={i18n.language === 'es' ? 'lang-active' : ''}
                    >
                        🇪🇸 Español
                    </Dropdown.Item>
                    <Dropdown.Item
                        onClick={() => changeLanguage('en')}
                        className={i18n.language === 'en' ? 'lang-active' : ''}
                    >
                        🇺🇸 English
                    </Dropdown.Item>
                    </Dropdown.Menu>
                </Dropdown>
                </ListGroup.Item>



                <ListGroup.Item as="li" bsPrefix=" ">
                    <Dropdown className="drp-user">
                        <Dropdown.Toggle as={Link} variant="link" to="#" id="dropdown-basic">
                            <i className="icon feather icon-settings" />
                        </Dropdown.Toggle>
                        <Dropdown.Menu alignRight className="profile-notification">
                            <div className="pro-head">
                                <img src={avatar1} className="img-radius" alt="User Profile" />
                                <span>
                                    {t('header.userMenu')}
                                </span>
                                <Link to="#" className="dud-logout" onClick={handleLogout} title={t('header.logout')}>
                                    <i className="feather icon-log-out" />
                                </Link>
                            </div>
                            <ListGroup as="ul" bsPrefix=" " variant="flush" className="pro-body">
                                <ListGroup.Item as="li" bsPrefix=" ">
                                    <Link to="#" className="dropdown-item">
                                        <i className="feather icon-settings" /> {t('header.settings')}
                                    </Link>
                                </ListGroup.Item>
                                <ListGroup.Item as="li" bsPrefix=" ">
                                    <Link to="/app/profile" className="dropdown-item">
                                        <i className="feather icon-user" /> {t('header.profile')}
                                    </Link>
                                </ListGroup.Item>
                                <ListGroup.Item as="li" bsPrefix=" ">
                                    <Link to="/app/messages" className="dropdown-item">
                                        <i className="feather icon-mail" /> {t('header.myMessages')}
                                    </Link>
                                </ListGroup.Item>
                                <ListGroup.Item as="li" bsPrefix=" ">
                                    <Link to="#" className="dropdown-item">
                                        <i className="feather icon-lock" /> {t('header.lockScreen')}
                                    </Link>
                                </ListGroup.Item>
                                <ListGroup.Item as="li" bsPrefix=" ">
                                    <Link to="#" className="dropdown-item" onClick={handleLogout}>
                                        <i className="feather icon-log-out" /> {t('header.logout')}
                                    </Link>
                                </ListGroup.Item>
                            </ListGroup>
                        </Dropdown.Menu>
                    </Dropdown>
                </ListGroup.Item>
            </ListGroup>
            <ChatList listOpen={listOpen} closed={() => setListOpen(false)} />
        </React.Fragment>
    );
};

export default NavRight;
