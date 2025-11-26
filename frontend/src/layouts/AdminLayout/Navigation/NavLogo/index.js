import React, { useContext } from 'react';
import { Link } from 'react-router-dom';

import { ConfigContext } from '../../../../contexts/ConfigContext';
import * as actionType from '../../../../store/actions';
import logo from '../../../../assets/images/logo.svg';

const NavLogo = () => {
    const configContext = useContext(ConfigContext);
    const { collapseMenu } = configContext.state;
    const { dispatch } = configContext;

    let toggleClass = ['mobile-menu'];
    if (collapseMenu) {
        toggleClass = [...toggleClass, 'on'];
    }

    return (
        <React.Fragment>
            <div className="navbar-brand header-logo">
                <Link to="#" className="b-brand">
                    <div className="b-bg" style={{ borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', width: '50px', height: '50px' }}>
                        <img className="rounded-circle" style={{ width: '40px' }} src={logo} alt="logo" />
                    </div>
                    <span className="b-title" style={{ whiteSpace: 'nowrap' }}>ITSC App</span>
                </Link>
                <Link
                    to="#"
                    className={toggleClass.join(' ')}
                    id="mobile-collapse"
                    onClick={() => dispatch({ type: actionType.COLLAPSE_MENU })}
                >
                    <span />
                </Link>
            </div>
        </React.Fragment>
    );
};

export default NavLogo;
