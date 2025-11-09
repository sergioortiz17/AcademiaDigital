import React from "react";
import { useTranslation } from "react-i18next";
import heroLogo from "../../../assets/images/hero-logo.svg";
import { Row, Col, Card } from "react-bootstrap";

const DashDefault = () => {
  const { t } = useTranslation();

  return (
    <React.Fragment>
      <Row>
        <Col>
          <h1 className="mb-4">{t("dashboard.title")}</h1>
        </Col>
      </Row>

      <Row>
        <Col>
          <Card className="card-social shadow-sm border-0">
            {/* LOGO */}
            <Card.Body className="border-bottom text-center py-4">
              <div className="d-flex justify-content-center align-items-center">
                <img
                  src={heroLogo}
                  alt="Instituto Técnico Superior Córdoba"
                  className="img-fluid"
                  style={{
                    maxWidth: "280px",
                    height: "auto",
                    display: "block",
                  }}
                />
              </div>
            </Card.Body>

            {/* PROGRESO */}
            <Card.Body style={{ background: "#eb9c00" }}>
              <div className="row align-items-center justify-content-center card-active">
                <div className="col-8 text-center">
                  <h4 className="mb-3 text-white">
                    {t("dashboard.progress", { progress: 100 })}
                  </h4>
                  <div className="progress" style={{ height: "6px" }}>
                    <div
                      className="progress-bar bg-success"
                      role="progressbar"
                      style={{ width: "100%" }}
                      aria-valuenow="100"
                      aria-valuemin="0"
                      aria-valuemax="100"
                    />
                  </div>
                </div>
              </div>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </React.Fragment>
  );
};

export default DashDefault;
