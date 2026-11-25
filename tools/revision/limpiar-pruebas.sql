-- T2-06: borra las sesiones creadas por tools/revision/prueba-asignador.ps1 y por la CI,
-- para que las pruebas no cuenten en las cuotas del estudio real.
-- Ejecútelo en el SQL Editor de Neon (rama «desarrollo») o con psql en el PostgreSQL local.
-- Los eventos, datos del participante y evaluaciones de esas sesiones se borran en cascada.

-- 1) Revise qué se va a borrar
SELECT consentimiento_version, estado, COUNT(*)
FROM sesiones_experimentales
WHERE consentimiento_version IN ('prueba-rev-tec-03', 'ci')
GROUP BY consentimiento_version, estado;

-- 2) Bórrelas
DELETE FROM sesiones_experimentales
WHERE consentimiento_version IN ('prueba-rev-tec-03', 'ci');
