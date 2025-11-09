import React from 'react';
import { useTranslation } from 'react-i18next';
import { Card, Row, Col } from 'react-bootstrap';

const Messages = () => {
    const { t } = useTranslation();

    return (
        <React.Fragment>
            <Row>
                <Col>
                    <Card>
                        <Card.Header>
                            <h5>{t('pages.messages.title')}</h5>
                        </Card.Header>
                        <Card.Body>
                            <p>{t('pages.messages.description')}</p>
                            <p className="text-muted">{t('pages.messages.comingSoon')}</p>
                        </Card.Body>
                    </Card>
                </Col>
            </Row>
        </React.Fragment>
    );
};

export default Messages;

